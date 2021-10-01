using System;
using PX.Data;

namespace PX.Objects.RUTROT
{
	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
	public interface IRUTROTable
	{
		bool? IsRUTROTDeductible
		{
			get;
			set;
		}

		string GetDocumentType();

		string GetDocumentNbr();

		bool? GetRUTROTCompleted();

		int? GetDocumentBranchID();

		string GetDocumentCuryID();

		bool? GetDocumentHold();

		IBqlTable GetBaseDocument();
	}

	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
	public interface IRUTROTableLine
	{
		int? GetInventoryID();

		bool? IsRUTROTDeductible
		{
			get;
			set;
		}

		string RUTROTItemType
		{
			get;
			set;
		}

		int? RUTROTWorkTypeID
		{
			get;
			set;
		}

		decimal? CuryRUTROTTaxAmountDeductible
		{
			get;
			set;
		}

		decimal? RUTROTTaxAmountDeductible
		{
			get;
			set;
		}

		decimal? CuryRUTROTAvailableAmt
		{
			get;
			set;
		}

		decimal? RUTROTAvailableAmt
		{
			get;
			set;
		}

		IBqlTable GetBaseDocument();
	}
}
