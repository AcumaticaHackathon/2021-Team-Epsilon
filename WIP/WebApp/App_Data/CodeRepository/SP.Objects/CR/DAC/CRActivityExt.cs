using System;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.EP;

namespace SP.Objects.CR
{
	public class CRActivityExt : PXCacheExtension<CRActivity>
	{
		#region Type
		public abstract class type : IBqlField { }
		protected string _Type;
		[PXDBString(5, IsFixed = true, IsUnicode = false)]
		[PXUIField(DisplayName = "Type", Required = true)]
		[PXSelector(typeof(EPActivityType.type), DescriptionField = typeof(EPActivityType.description))]
		[PXRestrictor(typeof(Where<EPActivityType.active, Equal<True>>), PX.Objects.CR.Messages.InactiveActivityType, typeof(EPActivityType.type))]
		[PXRestrictor(typeof(Where<EPActivityType.isInternal, NotEqual<True>>), PX.Objects.CR.Messages.ExternalActivityType, typeof(EPActivityType.type))]
		[PXDefault(typeof(Search<EPActivityType.type,
			Where<EPActivityType.isInternal, NotEqual<True>,
			And<EPActivityType.isDefault, Equal<True>,
			And<Current<CRActivity.classID>, Equal<CRActivityClass.activity>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual String Type { get; set; }
		#endregion

		#region CommentType
		public abstract class commenttype : IBqlField { }

		[PXString(5, IsFixed = true, IsUnicode = false)]
		[PXUIField(DisplayName = "Type", Required = true, Visible = false)]
		[PXSelector(typeof(EPActivityType.type), DescriptionField = typeof(EPActivityType.description))]
		[PXRestrictor(typeof(Where<EPActivityType.active, Equal<True>>), PX.Objects.CR.Messages.InactiveActivityType, typeof(EPActivityType.type))]
		[PXRestrictor(typeof(Where<EPActivityType.isInternal, NotEqual<True>>), PX.Objects.CR.Messages.InternalActivityType, typeof(EPActivityType.type))]
		[PXDefault(typeof(Search<EPActivityType.type,
			Where<EPActivityType.isInternal, NotEqual<True>,
			And<EPActivityType.isDefault, Equal<True>,
			And<Current<CRActivity.classID>, Equal<CRActivityClass.activity>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual string CommentType { get; set; }
		#endregion

        #region CommentSubject
        public abstract class commentSubject :IBqlField { }
		
        [PXString(255, InputMask = "", IsUnicode = true)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Summary", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String CommentSubject { get; set; }
        #endregion

        #region CommentBody
        public abstract class commentBody : IBqlField { }

        [PXString(IsUnicode = true)]
        [PXUIField(DisplayName = "Activity Details")]
        public virtual String CommentBody { get; set; }
        #endregion

        #region StartDate
        public abstract class commentStartDate : PX.Data.IBqlField { }

        [PXDateAndTime]
        [PXUIField(DisplayName = "Start Date", Required = true, Visible = false)]
        [PXDefault(typeof(AccessInfo.businessDate), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual DateTime? CommentStartDate { get; set; }
        #endregion
	}
}