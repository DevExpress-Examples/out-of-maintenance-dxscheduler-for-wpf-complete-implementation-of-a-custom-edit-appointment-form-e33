Imports Microsoft.VisualBasic
Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data
Imports DevExpress.Utils
Imports DevExpress.Xpf.Core
Imports DevExpress.Xpf.Editors
Imports DevExpress.Xpf.Scheduler
Imports DevExpress.Xpf.Scheduler.UI
Imports DevExpress.XtraScheduler
Imports DevExpress.XtraScheduler.Localization
Imports DevExpress.XtraScheduler.Native
Imports DevExpress.XtraScheduler.UI

Namespace SchedulerCompleteAppointmentFormWpf
	Partial Public Class CustomAppointmentForm
		Inherits UserControl
		Private ReadOnly controller_Renamed As CustomAppointmentFormController
		Private readOnly_Renamed As Boolean
		Private timeEditMask_Renamed As String = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern
		Private ReadOnly storage_Renamed As SchedulerStorage
		Private ReadOnly control_Renamed As SchedulerControl
		Private recurrenceVisualController_Renamed As RecurrenceVisualController

		Public Sub New(ByVal control As SchedulerControl, ByVal apt As Appointment, ByVal [readOnly] As Boolean)
			Guard.ArgumentNotNull(control, "control")
			Guard.ArgumentNotNull(control.Storage, "control.Storage")
			Guard.ArgumentNotNull(apt, "apt")
			Me.controller_Renamed = CreateController(control, apt)
			Me.control_Renamed = control
			Me.storage_Renamed = control.Storage
			Me.recurrenceVisualController_Renamed = New RecurrenceVisualController(Controller)
			Me.readOnly_Renamed = [readOnly]
			InitializeComponent()
			AddHandler cbRecurrence.EditValueChanged, AddressOf OnCbRecurrenceEditValueChanged
		End Sub
		Private Sub OnCbRecurrenceEditValueChanged(ByVal sender As Object, ByVal e As EditValueChangedEventArgs)
			UpdateLayout(Me)
		End Sub

		Private Shared Overloads Sub UpdateLayout(ByVal dialog As UserControl)
			Dim floatingContainer As FloatingContainer = FloatingContainer.GetFloatingContainer(dialog)
			If floatingContainer IsNot Nothing Then
				floatingContainer.UpdateAutoSize()
			End If
		End Sub

		#Region "Properties"
		#Region "Caption"
		Public Property Caption() As String
			Get
				Return CStr(GetValue(CaptionProperty))
			End Get
			Set(ByVal value As String)
				SetValue(CaptionProperty, value)
			End Set
		End Property
		Public Shared ReadOnly CaptionProperty As DependencyProperty = CreateCaptionProperty()
		Private Shared Function CreateCaptionProperty() As DependencyProperty
            Return DevExpress.Xpf.Core.Native.DependencyPropertyHelper.RegisterProperty(Of CustomAppointmentForm, String)("Caption", String.Empty, AddressOf ONCC, Nothing)
        End Function
        Private Shared Sub ONCC(ByVal d As CustomAppointmentForm, ByVal e As DevExpress.Xpf.Core.Native.DependencyPropertyChangedEventArgs(Of String))
            d.OnCaptionChanged(e.OldValue, e.NewValue)
        End Sub
		Private Sub OnCaptionChanged(ByVal oldValue As String, ByVal newValue As String)
			UpdateContainerCaption(newValue)
		End Sub
		#End Region
		Public ReadOnly Property Controller() As CustomAppointmentFormController
			Get
				Return controller_Renamed
			End Get
		End Property
		Public ReadOnly Property TimeEditMask() As String
			Get
				Return timeEditMask_Renamed
			End Get
		End Property
		Protected Friend ReadOnly Property Control() As SchedulerControl
			Get
				Return control_Renamed
			End Get
		End Property
		Protected Friend ReadOnly Property Storage() As SchedulerStorage
			Get
				Return storage_Renamed
			End Get
		End Property
		Protected Friend ReadOnly Property IsNewAppointment() As Boolean
			Get
				Return If(controller_Renamed IsNot Nothing, controller_Renamed.IsNewAppointment, True)
			End Get
		End Property
		Public ReadOnly Property TimeZoneHelper() As TimeZoneHelper
			Get
				Return Controller.TimeZoneHelper
			End Get
		End Property
		Public ReadOnly Property ShouldShowRecurrence() As Boolean
			Get
				Return Controller.ShouldShowRecurrence
			End Get
		End Property
		Public Property [ReadOnly]() As Boolean
			Get
				Return readOnly_Renamed
			End Get
			Set(ByVal value As Boolean)
				If readOnly_Renamed = value Then
					Return
				End If
				readOnly_Renamed = value
			End Set
		End Property
		Public ReadOnly Property RecurrenceVisualController() As RecurrenceVisualController
			Get
				Return recurrenceVisualController_Renamed
			End Get
		End Property
		#End Region
		Protected Overridable Function CreateController(ByVal control As SchedulerControl, ByVal apt As Appointment) As CustomAppointmentFormController
			Return New CustomAppointmentFormController(control, apt)
		End Function
		Private Sub OnOKButtonClick(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If edtEndDate.HasValidationError OrElse edtEndTime.HasValidationError Then
				Return
			End If
			Dim args As New ValidationArgs()
			SchedulerFormHelper.ValidateValues(Me, args)
			If (Not args.Valid) Then
				DXMessageBox.Show(args.ErrorMessage, System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OK, MessageBoxImage.Exclamation)
				FocusInvalidControl(args)
				Return
			End If
			SchedulerFormHelper.CheckForWarnings(Me, args)
			If (Not args.Valid) Then
				Dim answer As MessageBoxResult = DXMessageBox.Show(args.ErrorMessage, System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OKCancel, MessageBoxImage.Question)
				If answer = MessageBoxResult.Cancel Then
					FocusInvalidControl(args)
					Return
				End If
			End If
			If (Not controller_Renamed.IsConflictResolved()) Then
				DXMessageBox.Show(SchedulerLocalizer.GetString(SchedulerStringId.Msg_Conflict), System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OK, MessageBoxImage.Exclamation)
				Return
			End If
			If ShouldShowRecurrence Then
				RecurrenceVisualController.ApplyRecurrence()
			End If
			If CanApplyChanges() Then
				Controller.ApplyChanges()
			End If
			CloseForm(True)
		End Sub
		Protected Overridable Function CanApplyChanges() As Boolean
			Return Controller.IsNewAppointment OrElse controller_Renamed.IsAppointmentChanged()
		End Function
		Private Sub FocusInvalidControl(ByVal args As ValidationArgs)
			Dim control As Control = TryCast(args.Control, Control)
			If control IsNot Nothing Then
				control.Focus()
			End If
		End Sub
		Private Sub OnCancelButtonClick(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CloseForm(False)
		End Sub
		Private Sub OnDeleteButtonClick(ByVal sender As Object, ByVal e As RoutedEventArgs)
			If IsNewAppointment Then
				Return
			End If
			Controller.DeleteAppointment()
			CloseForm(False)
		End Sub
		Private Sub CloseForm(ByVal dialogResult As Boolean)
            SchedulerFormBehavior.Close(Me, dialogResult)
		End Sub
		Private Sub OnEdtEndTimeValidate(ByVal sender As Object, ByVal e As ValidationEventArgs)
			If e.Value Is Nothing Then
				Return
			End If
			e.IsValid = IsValidInterval(Controller.Start.Date, Controller.Start.Date.TimeOfDay, Controller.End.Date, (CDate(e.Value)).TimeOfDay)
			e.ErrorContent = SchedulerLocalizer.GetString(SchedulerStringId.Msg_InvalidEndDate)
		End Sub
		Private Sub OnEdtEndDateValidate(ByVal sender As Object, ByVal e As ValidationEventArgs)
			If e.Value Is Nothing Then
				Return
			End If
			e.IsValid = IsValidInterval(Controller.Start.Date, Controller.Start.Date.TimeOfDay, CDate(e.Value), Controller.End.TimeOfDay)
			e.ErrorContent = SchedulerLocalizer.GetString(SchedulerStringId.Msg_InvalidEndDate)
		End Sub
		Protected Friend Overridable Function IsValidInterval(ByVal startDate As DateTime, ByVal startTime As TimeSpan, ByVal endDate As DateTime, ByVal endTime As TimeSpan) As Boolean
			Return AppointmentFormControllerBase.ValidateInterval(startDate, startTime, endDate, endTime)
		End Function
		Protected Overrides Sub OnVisualParentChanged(ByVal oldParent As DependencyObject)
			MyBase.OnVisualParentChanged(oldParent)
			BindingOperations.ClearBinding(Me, CaptionProperty)
			If oldParent Is Nothing Then
				Dim captionBinding As New Binding()
				captionBinding.Path = New PropertyPath("Subject")
				captionBinding.Source = Controller
				SetBinding(CaptionProperty, captionBinding)
				UpdateContainerCaption(Controller.Subject)
			End If
		End Sub
		Private Sub UpdateContainerCaption(ByVal subject As String)
            SchedulerFormBehavior.SetTitle(Me, SchedulerUtils.FormatAppointmentFormCaption(Controller.AllDay, subject, False))
		End Sub
		Private Sub AptForm_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			AddHandler LayoutUpdated, AddressOf AppointmentForm_LayoutUpdated
		End Sub
		Private Sub AppointmentForm_LayoutUpdated(ByVal sender As Object, ByVal e As EventArgs)
			RemoveHandler LayoutUpdated, AddressOf AppointmentForm_LayoutUpdated
			subjectEdit.Focus()
		End Sub
	End Class

	Public Class CustomAppointmentFormController
		Inherits AppointmentFormController
		Public Sub New(ByVal control As SchedulerControl, ByVal apt As Appointment)
			MyBase.New(control, apt)
		End Sub
		Public Property Contact() As String
			Get
				Return GetContactValue(EditedAppointmentCopy)
			End Get
			Set(ByVal value As String)
				EditedAppointmentCopy.CustomFields("Contact") = value
			End Set
		End Property
		Private Property SourceContact() As String
			Get
				Return GetContactValue(SourceAppointment)
			End Get
			Set(ByVal value As String)
				SourceAppointment.CustomFields("Contact") = value
			End Set
		End Property

		Public Overrides Function IsAppointmentChanged() As Boolean
			If MyBase.IsAppointmentChanged() Then
				Return True
			End If
			Return SourceContact <> Contact
		End Function

		Protected Function GetContactValue(ByVal apt As Appointment) As String
			Return Convert.ToString(apt.CustomFields("Contact"))
		End Function

		Protected Overrides Sub ApplyCustomFieldsValues()
			SourceContact = Contact
		End Sub
	End Class
End Namespace
