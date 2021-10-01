using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{
	public interface ILocation
	{
		long? Id { get; set; }
		string Name { get; set; }
		bool? Active { get; set; }
	}
}
