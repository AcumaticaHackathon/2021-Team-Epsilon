<ActionBar>
	<CustomItems>                               
		<px:PXToolBarButton CommandName="DescriptionRowUp" CommandSourceID="ds">
			<Images Normal="main@ArrowUp" />
		</px:PXToolBarButton>
		<px:PXToolBarButton CommandName="DescriptionRowDown" CommandSourceID="ds">
			<Images Normal="main@ArrowDown" />
		</px:PXToolBarButton>
	</CustomItems>
</ActionBar>
<Levels>
	<px:PXGridLevel DataKeyNames="AttributeID" DataMember="DescriptionGenerationRules">
		<RowTemplate>
			<px:PXSelector ID="edDescriptionAttributeID" runat="server" DataField="AttributeID" AutoRefresh="true" FilterByAllFields="True" />
		</RowTemplate>
		<Columns>
			<px:PXGridColumn DataField="SegmentType" Type="DropDownList" CommitChanges="true" />
			<px:PXGridColumn DataField="AttributeID" TextAlign="Left" Width="135px" CommitChanges="true" />
			<px:PXGridColumn DataField="Constant" Width="100px" CommitChanges="true" />
			<px:PXGridColumn DataField="NumberingID" CommitChanges="true" />
			<px:PXGridColumn DataField="NumberOfCharacters" CommitChanges="true" />
			<px:PXGridColumn DataField="UseSpaceAsSeparator" TextAlign="Center" Type="CheckBox" Width="80px" CommitChanges="true" />
			<px:PXGridColumn DataField="Separator" Width="80px" CommitChanges="true" />
			<px:PXGridColumn DataField="AddSpaces" TextAlign="Center" Type="CheckBox" Width="70px" CommitChanges="true" />
		</Columns>
	</px:PXGridLevel>
</Levels>