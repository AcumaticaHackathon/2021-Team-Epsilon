using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Attributes
{
	public class MultiDocumentStatusListAttribute : PXStringListAttribute
	{
		protected Type[] _documentStatusFieldList;

		public MultiDocumentStatusListAttribute(params Type[] documentStatusFieldList)
		{
			if (documentStatusFieldList?.Length > 1 == false)
				throw new PXArgumentException(nameof(documentStatusFieldList));

			_documentStatusFieldList = documentStatusFieldList;
			base.IsLocalizable = false;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			FillDocumentStatusList(sender);
		}

		protected virtual void FillDocumentStatusList(PXCache cache)
		{
			if (_AllowedValues?.Any(v => v != null) != true)
			{
				var documentStatusList = new Dictionary<string, string>();
				foreach (var documentStatusField in _documentStatusFieldList)
					CopyDocumentStatusValues(cache.Graph, documentStatusField, documentStatusList);

				_AllowedValues = documentStatusList.Keys.ToArray();
				_AllowedLabels = documentStatusList.Values.ToArray();
			}
		}

		protected virtual void CopyDocumentStatusValues(PXGraph graph, Type documentStatusField, Dictionary<string, string> result)
		{
			var documentCache = graph.Caches[BqlCommand.GetItemType(documentStatusField)];
			var documentStatusList = documentCache.GetAttributesReadonly(documentStatusField.Name)
				.OfType<PXStringListAttribute>().FirstOrDefault();

			if (documentStatusList == null)
				return;

			result.AddRange(documentStatusList.ValueLabelDic.
				Select(documentStatus => new KeyValuePair<string, string>(
					GetDocumentStatusValue(documentCache, documentStatus.Key),
					$"{documentCache.DisplayName} - {PXMessages.LocalizeNoPrefix(documentStatus.Value)}")));
		}

		public virtual string GetDocumentStatusValue(PXCache documentCache, string documentStatusValue)
			=> $"{documentCache.GetName()}~{documentStatusValue}";
	}
}
