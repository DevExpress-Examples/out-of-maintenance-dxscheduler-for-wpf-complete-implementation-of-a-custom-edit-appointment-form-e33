using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DevExpress.Utils;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Scheduler;
using DevExpress.Xpf.Scheduler.UI;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Localization;
using DevExpress.XtraScheduler.Native;
using DevExpress.XtraScheduler.UI;

namespace SchedulerCompleteAppointmentFormWpf {
    public partial class CustomAppointmentForm : UserControl {
        readonly CustomAppointmentFormController controller;
		bool readOnly;
		string timeEditMask = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;
		readonly SchedulerStorage storage;
		readonly SchedulerControl control;
		RecurrenceVisualController recurrenceVisualController;
		public CustomAppointmentForm() {		  
		}
		public CustomAppointmentForm(SchedulerControl control, Appointment apt, bool readOnly) {
			Guard.ArgumentNotNull(control, "control");
			Guard.ArgumentNotNull(control.Storage, "control.Storage");
			Guard.ArgumentNotNull(apt, "apt");
			this.controller = CreateController(control, apt);
			this.control = control;
			this.storage = control.Storage;
			this.recurrenceVisualController = new RecurrenceVisualController(Controller);
			this.readOnly = readOnly;
			InitializeComponent();
			this.cbRecurrence.EditValueChanged += new EditValueChangedEventHandler(OnCbRecurrenceEditValueChanged);
		}
		void OnCbRecurrenceEditValueChanged(object sender, EditValueChangedEventArgs e) {
			UpdateLayout(this);
		}

        static void UpdateLayout(UserControl dialog) {
            FloatingContainer floatingContainer = FloatingContainer.GetFloatingContainer(dialog);
            if (floatingContainer != null) {
                floatingContainer.UpdateAutoSize();
            }
        }

		#region Properties
		#region Caption
		public string Caption {
			get { return (string)GetValue(CaptionProperty); }
			set { SetValue(CaptionProperty, value); }
		}
		public static readonly DependencyProperty CaptionProperty = CreateCaptionProperty();
		static DependencyProperty CreateCaptionProperty() {
			return DevExpress.Xpf.Core.Native.DependencyPropertyHelper.RegisterProperty<CustomAppointmentForm, string>("Caption", String.Empty, (d, e) => d.OnCaptionChanged(e.OldValue, e.NewValue), null);
		}
		void OnCaptionChanged(string oldValue, string newValue) {
			UpdateContainerCaption(newValue);
		}
		#endregion
		public CustomAppointmentFormController Controller { get { return controller; } }
		public string TimeEditMask { get { return timeEditMask; } }
		protected internal SchedulerControl Control { get { return control; } }
		protected internal SchedulerStorage Storage { get { return storage; } }
		protected internal bool IsNewAppointment { get { return controller != null ? controller.IsNewAppointment : true; } }
		public TimeZoneHelper TimeZoneHelper { get { return Controller.TimeZoneHelper; } }
		public bool ShouldShowRecurrence {
			get {
				return Controller.ShouldShowRecurrence;
			}
		}
		public bool ReadOnly {
			get { return readOnly; }
			set {
				if (readOnly == value)
					return;
				readOnly = value;
			}
		}
		public RecurrenceVisualController RecurrenceVisualController { get { return recurrenceVisualController; } }
		#endregion
        protected virtual CustomAppointmentFormController CreateController(SchedulerControl control, Appointment apt) {
            return new CustomAppointmentFormController(control, apt);			
		}
		void OnOKButtonClick(object sender, RoutedEventArgs e) {
			if(edtEndDate.HasValidationError || edtEndTime.HasValidationError)
				return;
			ValidationArgs args = new ValidationArgs();
			SchedulerFormHelper.ValidateValues(this, args);
			if(!args.Valid) {
				DXMessageBox.Show(args.ErrorMessage, System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OK, MessageBoxImage.Exclamation);
				FocusInvalidControl(args);
				return;
			}
			SchedulerFormHelper.CheckForWarnings(this, args);
			if(!args.Valid) {
				MessageBoxResult answer = DXMessageBox.Show(args.ErrorMessage, System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OKCancel, MessageBoxImage.Question);
				if(answer == MessageBoxResult.Cancel) {
					FocusInvalidControl(args);
					return;
				}
			}
			if (!controller.IsConflictResolved()) {
				DXMessageBox.Show(SchedulerLocalizer.GetString(SchedulerStringId.Msg_Conflict), System.Windows.Forms.Application.ProductName, System.Windows.MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			if(ShouldShowRecurrence) 
				RecurrenceVisualController.ApplyRecurrence();
			Controller.ApplyChanges();
			CloseForm(true);
		}
		protected virtual bool CanApplyChanges() {
			return Controller.IsNewAppointment || controller.IsAppointmentChanged();
		}
		void FocusInvalidControl(ValidationArgs args) {
			Control control = args.Control as Control;
			if(control != null) 
				control.Focus();
		}
		void OnCancelButtonClick(object sender, RoutedEventArgs e) {
			CloseForm(false);
		}
		void OnDeleteButtonClick(object sender, RoutedEventArgs e) {
			if (IsNewAppointment)
				return;
			Controller.DeleteAppointment();
			CloseForm(false);
		}	 
		private void CloseForm(bool dialogResult) {
			FormOperationHelper.CloseDialog(this, dialogResult);
		}
		void OnEdtEndTimeValidate(object sender, ValidationEventArgs e) {
			if(e.Value == null)
				return;
			e.IsValid = IsValidInterval(Controller.Start.Date, Controller.Start.Date.TimeOfDay, Controller.End.Date, ((DateTime)e.Value).TimeOfDay);
			e.ErrorContent = SchedulerLocalizer.GetString(SchedulerStringId.Msg_InvalidEndDate);
		}
		void OnEdtEndDateValidate(object sender, ValidationEventArgs e) {
			if(e.Value == null)
				return;
			e.IsValid = IsValidInterval(Controller.Start.Date, Controller.Start.Date.TimeOfDay, (DateTime)e.Value, Controller.End.TimeOfDay);
			e.ErrorContent = SchedulerLocalizer.GetString(SchedulerStringId.Msg_InvalidEndDate);
		}
		protected internal virtual bool IsValidInterval(DateTime startDate, TimeSpan startTime, DateTime endDate, TimeSpan endTime) {
			return AppointmentFormControllerBase.ValidateInterval(startDate, startTime, endDate, endTime);
		}
		protected override void OnVisualParentChanged(DependencyObject oldParent) {
			base.OnVisualParentChanged(oldParent);
			BindingOperations.ClearBinding(this, CaptionProperty);
			if(oldParent == null) {
				Binding captionBinding = new Binding();
				captionBinding.Path = new PropertyPath("Subject");
				captionBinding.Source = Controller;
				SetBinding(CaptionProperty, captionBinding);
				UpdateContainerCaption(Controller.Subject);
			}
		}
		void UpdateContainerCaption(string subject) {
			FormOperationHelper.SetFormCaption(this, SchedulerUtils.FormatAppointmentFormCaption(Controller.AllDay, subject, false));
		}
		private void AptForm_Loaded(object sender, RoutedEventArgs e) {
			LayoutUpdated += new EventHandler(AppointmentForm_LayoutUpdated);
		}
		void AppointmentForm_LayoutUpdated(object sender, EventArgs e) {
			LayoutUpdated -= new EventHandler(AppointmentForm_LayoutUpdated);
			subjectEdit.Focus();
		}
	}

    public class CustomAppointmentFormController : AppointmentFormController {
        public CustomAppointmentFormController(SchedulerControl control, Appointment apt)
            : base(control, apt) {
        }
        public string Contact {
            get { return GetContactValue(EditedAppointmentCopy); }
            set { EditedAppointmentCopy.CustomFields["Contact"] = value; }
        }
        string SourceContact {
            get { return GetContactValue(SourceAppointment); }
            set { SourceAppointment.CustomFields["Contact"] = value; }
        }

        public override bool IsAppointmentChanged() {
            if (base.IsAppointmentChanged())
                return true;
            return SourceContact != Contact;
        }

        protected string GetContactValue(Appointment apt) {
            return Convert.ToString(apt.CustomFields["Contact"]);
        }

        protected override void ApplyCustomFieldsValues() {
            SourceContact = Contact;
        }
    }
}
