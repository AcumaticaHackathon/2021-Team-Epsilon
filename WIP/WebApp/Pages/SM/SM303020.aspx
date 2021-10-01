<%@ Page Language="C#" CodeFile="SM303020.aspx.cs" Inherits="Pages_SM303020" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true" ValidateRequest="false" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.OidcClient.OidcProviderMaint" PrimaryView="Providers">
        <CallbackCommands>
			<px:PXDSCallbackCommand Name="ChangeName" CommitChanges="True" />
            <px:PXDSCallbackCommand Name="ViewRedirectURI" CommitChanges="True" />
            <px:PXDSCallbackCommand Name="ViewProviderMetadata" CommitChanges="True" />
		</CallbackCommands>
    </px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">


    <px:PXTab ID="tab" runat="server" Width="100%" DataMember="Providers" DataSourceID="ds" >
        <Items>
            <px:PXTabItem Text="General settings">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" LabelsWidth="SM" StartColumn="True" />
                    <px:PXSelector ID="edName" runat="server" DataField="Name" DataSourceID="ds" AutoRefresh="True">
                        <GridProperties>
                            <Columns>
							    <px:PXGridColumn DataField="Name" Width="300px" />
							    <px:PXGridColumn DataField="IssuerIdentifier" Width="300px" />
							    <px:PXGridColumn DataField="Active" Type="CheckBox" />
						    </Columns>
                        </GridProperties>
                    </px:PXSelector>
                    <px:PXTextEdit ID="edIssuerIdentifier" runat="server" DataField="IssuerIdentifier" />
                    <px:PXCheckBox ID="edActive" runat="server" DataField="Active" Text="Active" />                    
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Authentication settings">
                <Template>
                    <px:PXLayoutRule runat="server" StartRow="True" ControlSize="XL" LabelsWidth="M" StartColumn="True" />
                    <px:PXTextEdit ID="edAuthorizationEndpoint" runat="server" DataField="AuthorizationEndpoint" />
                    <px:PXDropDown ID="edFlow" runat="server" DataField="Flow" CommitChanges="true" />
                    <px:PXDropDown ID="edResponseType" runat="server" DataField="ResponseType" />
                    <px:PXDropDown ID="edResponseMode" runat="server" DataField="ResponseMode" />
                    <px:PXTextEdit ID="edTokenEndpoint" runat="server" DataField="TokenEndpoint" />
                    <px:PXTextEdit ID="edJWKSetLocation" runat="server" DataField="JWKSetLocation" />
                    <px:PXTextEdit ID="edClientId" runat="server" DataField="ClientId" />
                    <px:PXTextEdit ID="edClientSecret" runat="server" DataField="ClientSecret" />
                    <px:PXTextEdit ID="edScopes" runat="server" DataField="Scopes" />
                    <px:PXTextEdit ID="edUserIdentityClaimType" runat="server" DataField="UserIdentityClaimType" />
                </Template>
            </px:PXTabItem>
            <px:PXTabItem Text="Icons">
                <Template>
                    <px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XL" GroupCaption="Icon" StartGroup="True"  StartColumn="True"/>
					<px:PXLabel ID="lblIconImgUploader" runat="server">Recommended Size: Width 100px, Height 100px</px:PXLabel>
                    <px:PXImageUploader ID="edIcon" runat="server" DataField="Icon" Height="120px" Width="420px" AllowUpload="True" 
                        SuppressLabel="True" AllowNoImage="true" />
                    <px:PXTextEdit ID="lblIcon" runat="server" DataField="IconGetter" Width="322px" Enabled="False" />
                </Template>
            </px:PXTabItem>
        </Items>
        <AutoSize Container="Window" Enabled="True" MinHeight="150" />
    </px:PXTab>  

    <px:PXSmartPanel ID="pnlChangeID" runat="server" Caption="Specify New Name" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="ChangeIDDialog" CreateOnDemand="false" AutoCallBack-Enabled="true" AcceptButtonID="btnOK"
        AutoCallBack-Target="formChangeID" AutoCallBack-Command="Refresh" CallBackMode-CommitChanges="True" CallBackMode-PostData="Page">
        <px:PXFormView ID="formChangeID" runat="server" DataSourceID="ds" Style="z-index: 100" Width="100%" CaptionVisible="False" DataMember="ChangeIDDialog">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlAcctCD" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
                <px:PXTextEdit ID="edAcctCD" runat="server" DataField="CD" />
            </Template>
        </px:PXFormView>
        <px:PXPanel ID="pnlChangeIDButton" runat="server" SkinID="Buttons">
            <px:PXButton ID="btnOK" runat="server" DialogResult="OK" Text="OK">
                <AutoCallBack Target="formChangeID" Command="Save" />
            </px:PXButton>
            <px:PXButton ID="PXButton3" runat="server" DialogResult="Cancel" Text="Cancel" />
        </px:PXPanel>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="pnlRedirectURI" runat="server" Caption="Redirect URI" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="StaticInfo" CreateOnDemand="false" Width="500px">
        <px:PXFormView ID="formRedirectURI" runat="server" DataSourceID="ds" Width="100%" CaptionVisible="False" DataMember="StaticInfo">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlRedirectURI" runat="server" StartColumn="True" SuppressLabel="true" ControlSize="XL" />
                <px:PXTextEdit ID="edRedirectURI" runat="server" DataField="RedirectURI" Width="430px" SuppressLabel="true" Enabled="false" style="margin-bottom: 20px;" />                
                <px:PXButton ID="btnCpy" runat="server" Text="Copy" AlignLeft="true">
                    <ClientEvents Click="copy_Click" />
                </px:PXButton>
            </Template>
        </px:PXFormView>   
        <script type="text/javascript">
            function copy_Click() {
                try {
                    var redirectUriInput = px_alls["edRedirectURI"];
                    redirectUriInput.select();
                    document.execCommand("copy");
                }
                catch (e) {

                }
            }
        </script>
    </px:PXSmartPanel>

    <px:PXSmartPanel ID="pnlMetadata" runat="server" Caption="Provider Metadata" CaptionVisible="true" DesignView="Hidden" LoadOnDemand="true" 
        Key="MetadataDocumentInfo" CreateOnDemand="false" Width="800px" CloseButtonDialogResult="OK" AutoCallBack-Enabled="true" >
        <px:PXFormView ID="formMetadata" runat="server" DataSourceID="ds" Width="100%" CaptionVisible="False" DataMember="MetadataDocumentInfo">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXLayoutRule ID="rlRedirectURI" runat="server" StartColumn="True" ControlSize="XL" />
                <px:PXTextEdit ID="edMetadataURI" runat="server" DataField="MetadataURI" Enabled="false" Width="550px" style="margin-bottom: 20px;"/>
                <px:PXLabel ID="lblMetadataDocument" runat="server" Text="Metadata Document:" />
                <px:PXTextEdit ID="edMetadataDocument" runat="server" DataField="MetadataDocument" Enabled="false" SuppressLabel="true"
                    TextMode="MultiLine" Width="700px" Height="400px" />
            </Template>
        </px:PXFormView>
    </px:PXSmartPanel>
    
</asp:Content>
