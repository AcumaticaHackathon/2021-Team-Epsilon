using PX.Data;
using PX.Objects.GL;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Standard BOM ID field attribute
    /// </summary>
    [PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
    [PXUIField(DisplayName = "BOM ID", Visibility = PXUIVisibility.SelectorVisible)]
    public class BomIDAttribute : AcctSubAttribute
    {

    }
}
