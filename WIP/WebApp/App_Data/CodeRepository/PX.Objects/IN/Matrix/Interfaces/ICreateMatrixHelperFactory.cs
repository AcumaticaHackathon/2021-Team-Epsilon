using PX.Data;
using PX.Objects.IN.Matrix.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN.Matrix.Interfaces
{
	public interface ICreateMatrixHelperFactory
	{
		CreateMatrixItemsHelper GetCreateMatrixItemsHelper();
	}
}
