namespace PX.Objects.PR
{
	public struct CertifiedJobKey
	{
		public int ProjectID;
		public int LaborItemID;

		public CertifiedJobKey(int projectID, int laborItemID)
		{
			ProjectID = projectID;
			LaborItemID = laborItemID;
		}
	}
}
