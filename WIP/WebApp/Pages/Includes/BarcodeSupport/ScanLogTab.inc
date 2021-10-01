<px:PXTabItem Text="Scan Log" BindingContext="formHeader" VisibleExp="DataControls[&quot;chkShowLog&quot;].Value == false" >
    <Template>
        <px:PXGrid ID="gridScanLog" runat="server" DataSourceID="ds" Width="100%" SkinID="Inquire" OnRowDataBound="LogGrid_RowDataBound">
            <Levels>
                <px:PXGridLevel DataMember="Logs">
                    <Columns>
                        <px:PXGridColumn DataField="ScanTime" />
                        <px:PXGridColumn DataField="Mode" />
                        <px:PXGridColumn DataField="Prompt" />
                        <px:PXGridColumn DataField="Scan" />
                        <px:PXGridColumn DataField="Message" />
                    </Columns>
                </px:PXGridLevel>
            </Levels>
            <AutoSize Container="Window" Enabled="True" MinHeight="400" />
        </px:PXGrid>
    </Template>
</px:PXTabItem>