using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

namespace PX.Objects.AM.Attributes
{
	/// <summary>
	/// <see cref="PXRestrictorAttribute"/> for <see cref="AMBomItem"/> to restrict by boms which contain at least one active revision
	/// </summary>
	public class ActiveBOMRestrictorAttribute : PXRestrictorAttribute
    {
        public ActiveBOMRestrictorAttribute() 
            : base(typeof(Where<AMBomItem.status, Equal<AMBomStatus.active>>), 
                  Messages.BomIsNotActive, 
                  typeof(AMBomItem.bOMID))
        {
        }

		public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (!e.ExternalCall || e.NewValue == null)
			{
				return;
			}

			var activeBom = SelectFrom<AMBomItemActiveAggregate>
				.Where<AMBomItemActiveAggregate.bOMID.IsEqual<@P.AsString>>
				.View.Select(sender.Graph, e.NewValue).TopFirst;

			if(activeBom?.BOMID != null)
            {
				// bom has at least one active record
				return;
            }

			object errorValue = e.NewValue;
			sender.RaiseFieldSelecting(_FieldName, e.Row, ref errorValue, false);
			PXFieldState state = errorValue as PXFieldState;
			e.NewValue = state != null ? state.Value : errorValue;

			throw new PXSetPropertyException(Messages.BomIsNotActive, e.NewValue);
		}
    }
}