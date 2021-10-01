using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Common;
using PX.Data;
using PX.SM;


namespace SP.Objects.SP
{
	public class SPFeedback : PXGraphExtension<KBFeedbackMaint>
	{
		public override void Initialize()
		{
			Base.Actions["Submit"].SetVisible(true);
			Base.Actions["Close"].SetVisible(false);
		}
		
		[PXDBString()]
		[FeedBackIsFind()]
		[PXUIField(DisplayName = "Did you find what you were looking for")]
		protected virtual void KBFeedback_IsFind_CacheAttached(PXCache sender)
		{
		}

		[PXDBString()]
		[FeedBackSatisfaction()]
		[PXUIField(DisplayName = "Please rate your overall satisfaction with Self-Service Portal")]
		protected virtual void KBFeedback_Satisfaction_CacheAttached(PXCache sender)
		{
		}
	}
}
