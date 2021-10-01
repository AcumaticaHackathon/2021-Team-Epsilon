<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AP301100.aspx.cs" Inherits="Page_AP301100" Title="Untitled Page" %>
<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" Runat="Server">
    <link rel="stylesheet" type="text/css" href='<%=ResolveClientUrl("~/Scripts/documentRecognition/pdfViewer/pdfViewer.css")%>' />
    <link rel="stylesheet" type="text/css" href='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognition.css")%>' />
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/pdf.js/pdf.min.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/pdfViewer/pdfViewer.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedRectangle.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedValue.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedColumn.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedTable.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedValueScroller.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/recognizedValueMapper.js")%>' type="text/javascript"></script>
    <script src='<%=ResolveClientUrl("~/Scripts/documentRecognition/recognition/feedbackCollector.js")%>' type="text/javascript"></script>
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" TypeName="PX.Objects.AP.InvoiceRecognition.APInvoiceRecognitionEntry" PrimaryView="Document" HeaderDescriptionField="Subject">
		<CallbackCommands>
            <px:PXDSCallbackCommand Name="Insert" Visible="true" />
            <px:PXDSCallbackCommand Name="Cancel" Visible="false" />
            <px:PXDSCallbackCommand Name="ContinueSave" Visible="true" CommitChanges="true" />
            <px:PXDSCallbackCommand Name="Delete" Visible="true" />

            <px:PXDSCallbackCommand Name="Save" Visible="false" />
            <px:PXDSCallbackCommand Name="SaveClose" Visible="false" />
            <px:PXDSCallbackCommand Name="CopyPaste" Visible="false" />
            <px:PXDSCallbackCommand Name="First" Visible="false" />
            <px:PXDSCallbackCommand Name="Previous" Visible="false" />
            <px:PXDSCallbackCommand Name="Next" Visible="false" />
            <px:PXDSCallbackCommand Name="Last" Visible="false" />
            <px:PXDSCallbackCommand Visible="false" Name="LinkLine" CommitChanges="true" DependOnGrid="grid" />
            <px:PXDSCallbackCommand Name="DumpTableFeedback" Visible="false" />
		</CallbackCommands>
        <ClientEvents Initialize="captureDatasource" CommandPerformed="commandPerformed" />
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" Runat="Server">
    <px:PXSplitContainer runat="server" ID="PXSplitContainer1" SplitterPosition="480">
        <Template1>
            <px:PXFormView ID="edDocument" runat="server" DataSourceID="ds" Width="100%" DataMember="Document" Height="100%" FilesIndicator="True"
                ShowDragNDropUploadWarning="true">
                <Template>
                    <px:PXLayoutRule runat="server" LabelsWidth="SM" ControlSize="XM" />
                    <px:PXTextEdit ID="RecognizedRecordRefNbr" runat="server" DataField="RecognizedRecordRefNbr" IsClientControl="False" />
                    <px:PXDropDown ID="RecognitionStatus" runat="server" DataField="RecognitionStatus" IsClientControl="False" />
                    <px:PXDropDown ID="DocType" runat="server" DataField="DocType" IsClientControl="False" CommitChanges="true" />
                    <px:PXSegmentMask CommitChanges="True" ID="VendorID" runat="server" DataField="VendorID" AllowAddNew="True" AllowEdit="True" DataSourceID="ds" AutoRefresh="True" IsClientControl="False" />
                    <px:PXSegmentMask CommitChanges="True" ID="VendorLocationID" runat="server" AutoRefresh="True" DataField="VendorLocationID" DataSourceID="ds" IsClientControl="False" />
                    <px:PXDateTimeEdit CommitChanges="True" ID="DocDate" runat="server" DataField="DocDate" Size="XM" IsClientControl="False" />
                    <px:PXDateTimeEdit CommitChanges="True" ID="DueDate" runat="server" DataField="DueDate" Size="XM" IsClientControl="False" />
                    <px:PXTextEdit ID="InvoiceNbr" runat="server" DataField="InvoiceNbr" IsClientControl="False" />
                    <px:PXTextEdit ID="DocDesc" runat="server" DataField="DocDesc" IsClientControl="False" />
                    <px:PXNumberEdit ID="CuryLineTotal" runat="server" DataField="CuryLineTotal" Enabled="False" Size="XM" IsClientControl="False" />
                    <px:PXNumberEdit ID="CuryOrigDocAmt" runat="server" CommitChanges="True" DataField="CuryOrigDocAmt" Size="XM" IsClientControl="False" />

                    <px:PXLayoutRule runat="server" SuppressLabel="true" />
                    <px:PXTextEdit ID="AllowFiles" runat="server" CommitChanges="true" DataField="AllowFiles" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXTextEdit ID="AllowFilesMsg" runat="server" CommitChanges="true" DataField="AllowFilesMsg" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXCheckBox ID="AllowUploadFile" runat="server" CommitChanges="true" DataField="AllowUploadFile" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXTextEdit ID="FileID" runat="server" CommitChanges="true" DataField="FileID" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXTextEdit ID="RecognizedDataJson" runat="server" CommitChanges="true" DataField="RecognizedDataJson" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXTextEdit ID="VendorTermJson" runat="server" CommitChanges="true" DataField="VendorTermJson" IsClientControl="False" Enabled="false" Style="display: none;" />
                    <px:PXFormView ID="edRecognizedRecord" runat="server" DataSourceID="ds"  Width="100%" DataMember="RecognizedRecords" Height="100%">
                        <Template>
                            <px:PXTextEdit ID="hiddenRefNbr" runat="server" DataField="RefNbr" IsClientControl="False" />
                        </Template>
                    </px:PXFormView>
                </Template>
                <ClientEvents Initialize="capturePrimaryForm" AfterRepaint="renderPdf" />
            </px:PXFormView>
        </Template1>
        <Template2>
            <div id="dragNdropContainer" style="width: 96%; border: 1px dashed; height: 92%; margin: 2%; display: flex; align-items: center; justify-content: center;">
                <img src='<%=ResolveClientUrl("~/Content/svg_icons/dragNdrop.svg")%>' style="transform: scale(2.0);" />
            </div>
            <div id="pdfRecognitionViewerContainer" style="width: 100%; height: 100%; display: flex; flex-flow: column;">
            </div>
            <px:PXLabel runat="server" ID="toShowSplitter2" Text=" " />
        </Template2>
        <AutoSize Enabled="true" Container="Window" />
    </px:PXSplitContainer>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="edItems" runat="server" DataSourceID="ds" Width="100%" Height="100%" NoteIndicator="false" FilesIndicator="false" AdjustPageSize="Auto"
        SkinID="Details" AutoAdjustColumns="true" AllowPaging="false" SyncPosition="true">
        <Levels>
            <px:PXGridLevel DataMember="Transactions">
                <Columns>
                    <px:PXGridColumn DataField="AlternateID" CommitChanges="true" Width="110px" />
                    <px:PXGridColumn DataField="InventoryID" AutoCallBack="True" LinkCommand="ViewItem" />
                    <px:PXGridColumn DataField="TranDesc" AutoCallBack="True" />
                    <px:PXGridColumn DataField="Qty" TextAlign="Right" AutoCallBack="True" />
                    <px:PXGridColumn DataField="UOM" AutoCallBack="True" />
                    <px:PXGridColumn DataField="CuryUnitCost" TextAlign="Right" AutoCallBack="True" CommitChanges="true" />
                    <px:PXGridColumn DataField="CuryLineAmt" TextAlign="Right" AutoCallBack="True" CommitChanges="true" />
                    <px:PXGridColumn DataField="CuryTranAmt" TextAlign="Right" />
                    <px:PXGridColumn DataField="RecognizedPONumber" TextAlign="Right" />
                    <px:PXGridColumn DataField="PONumberJson" />
                    <px:PXGridColumn DataField="ReceiptNbr" TextAlign="Right" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Enabled="true" MinHeight="230" />
        <ClientEvents Initialize="captureDetailsGrid" />
		<ActionBar>
			<CustomItems>
				<px:PXToolBarButton>
					<AutoCallBack Command="LinkLine" Target="ds" />
				</px:PXToolBarButton>
			</CustomItems>
		</ActionBar>
	</px:PXGrid>
    	<px:PXSmartPanel ID="PanelLinkLine" runat="server" Width="1100px" Height="520px" Key="linkLineFilter" CommandSourceID="ds" Caption="Link Line" CaptionVisible="True" LoadOnDemand="False" AutoCallBack-Command="Refresh" AutoCallBack-Target="LinkLineFilterForm">
		<px:PXFormView runat="server" ID="LinkLineFilterForm" DataMember="linkLineFilter" Style="z-index: 100;" Width="100%" CaptionVisible="false" SkinID="Transparent">
			<Template>
				<px:PXLayoutRule ID="PXLayoutRule02" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="XM" />
				<px:PXSelector runat="server" ID="edPOOrderNbr" DataField="POOrderNbr" CommitChanges="True" AutoRefresh="True" />
				<px:PXSelector runat="server" ID="edInventoryID" DataField="InventoryID" />
				<px:PXSelector runat="server" ID="edUOM" DataField="UOM" />
				<px:PXLayoutRule ID="PXLayoutRule01" runat="server" StartColumn="True" LabelsWidth="S" ControlSize="SM" />
			</Template>
		</px:PXFormView>
		<px:PXGrid ID="LinkLineGrid" runat="server" Height="200px" Width="100%" BatchUpdate="False" SkinID="Inquire" DataSourceID="ds" FastFilterFields="PONbr,ReceiptNbr,POReceipt__InvoiceNbr,TranDesc,SiteID" TabIndex="17500" FilesIndicator="False" NoteIndicator="False"
			Caption="Receipt" CaptionVisible="false" AdjustPageSize="Auto">
			<AutoSize Enabled="true" />
			<Levels>
				<px:PXGridLevel DataMember="linkLineReceiptTran">
					<Columns>
						<px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="False" AllowSort="False" AllowMove="False" TextAlign="Center" AutoCallBack="True" CommitChanges="True" AllowResize="False" AllowShowHide="False" />
						<px:PXGridColumn DataField="PONbr" />
						<px:PXGridColumn DataField="POType" />
						<px:PXGridColumn DataField="ReceiptNbr" />
						<px:PXGridColumn DataField="POReceipt__InvoiceNbr" />
						<px:PXGridColumn DataField="SubItemID" />
						<px:PXGridColumn DataField="SiteID" />
						<px:PXGridColumn DataField="LineNbr" />
						<px:PXGridColumn DataField="CuryID" />
						<px:PXGridColumn DataField="ReceiptQty" />
						<px:PXGridColumn DataField="CuryExtCost" />
						<px:PXGridColumn DataField="UnbilledQty" />
						<px:PXGridColumn DataField="TranDesc" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<Mode AllowAddNew="False" AllowDelete="False" />
		</px:PXGrid>
		<px:PXGrid ID="LinkLineOrderGrid" runat="server" Height="200px" Width="100%" BatchUpdate="False" SkinID="Inquire" DataSourceID="ds" FastFilterFields="POOrder__OrderNbr,POOrder__VendorRefNbr,TranDesc,SiteID" TabIndex="17500" FilesIndicator="False" NoteIndicator="False"
			Caption="Order" CaptionVisible="false" AdjustPageSize="Auto">
			<AutoSize Enabled="true" />
			<Levels>
				<px:PXGridLevel DataMember="linkLineOrderTran">
					<Columns>
						<px:PXGridColumn DataField="Selected" Type="CheckBox" AllowCheckAll="False" AllowSort="False" AllowMove="False" TextAlign="Center" AutoCallBack="True" CommitChanges="True" AllowResize="False" AllowShowHide="False" />
						<px:PXGridColumn DataField="POOrder__OrderNbr" />
						<px:PXGridColumn DataField="POOrder__OrderType" />
						<px:PXGridColumn DataField="LineNbr" />
						<px:PXGridColumn DataField="POOrder__VendorRefNbr" />
						<px:PXGridColumn DataField="SubItemID" />
						<px:PXGridColumn DataField="SiteID" />
						<px:PXGridColumn DataField="POOrder__CuryID" />
						<px:PXGridColumn DataField="OrderQty" />
						<px:PXGridColumn DataField="curyLineAmt" />
						<px:PXGridColumn DataField="UnbilledQty" />
						<px:PXGridColumn DataField="CuryUnbilledAmt" />
						<px:PXGridColumn DataField="TranDesc" />
					</Columns>
				</px:PXGridLevel>
			</Levels>
			<Mode AllowAddNew="False" AllowDelete="False" />
		</px:PXGrid>
		<px:PXPanel ID="LinkLineButtons" runat="server" SkinID="Buttons">
			<px:PXButton ID="LinkLineSave" runat="server" DialogResult="Yes" Text="Save" />
			<px:PXButton ID="LinkLineCancel" runat="server" DialogResult="Cancel" Text="Cancel" />
		</px:PXPanel>
	</px:PXSmartPanel>
    <px:PXSmartPanel ID="edFeedbackPanel" runat="server" Key="BoundFeedback" Width="500px" Height="135px"
        Caption="Bound Feedback" CaptionVisible="true">
        <px:PXFormView ID="edFeedback" runat="server" DataSourceID="ds" Width="100%" DataMember="BoundFeedback" CaptionVisible="False">
            <ContentStyle BackColor="Transparent" BorderStyle="None" />
            <Template>
                <px:PXTextEdit ID="edFieldBound" runat="server" CommitChanges="true" DataField="FieldBound" IsClientControl="False" Enabled="false" />
                <px:PXTextEdit ID="edTableRelated" runat="server" CommitChanges="true" DataField="TableRelated" IsClientControl="False" Enabled="false" />
            </Template>
            <AutoSize Enabled="true" />
        </px:PXFormView>
    </px:PXSmartPanel>
    <script type="text/javascript">
        let datasource = null;
        let detailsGrid = null;
        let primaryForm = null;
        let viewBar = null;
        let pdfRecognitionViewer = null;
        let callbackManager = null;

        let isPdfRendered = false;
        let isDataProcessed = false;
        let prevFilesCount = null;
        let prevFileId = null;

        function captureDatasource(ds) {
            datasource = ds;
        }

        function capturePrimaryForm(form) {
            hideSynchronizationControls();

            primaryForm = form;

            subscribeOnViewBarEvents(form);
        }

        function findViewBar(form) {
            if (viewBar) {
                return;
            }

            const all = __px_all(form);

            for (var c in all) {
                if (all[c].__className !== "PXDataViewBar") {
                    continue;
                }

                viewBar = all[c];
                break;
            }

            callbackManager = __px_callback(viewBar);
            if (!callbackManager) {
                return;
            }

            callbackManager.addHandler(function (context) {
                if (context.controlID !== viewBar.ID) {
                    return;
                }

                if (context.info.name !== 'FilesMenuShow') {
                    return;
                }

                let fileUploader = px_all[viewBar.ID + "_fb_upld2"];
                if (fileUploader) {
                    let allowedFilesControl = px_all[allowFilesId];
                    if (allowedFilesControl) {
                        fileUploader.allowedFiles = [allowedFilesControl.getValue()];
                    }
                }
            });
        }

        function subscribeOnViewBarEvents(form) {
            findViewBar(form);

            if (!viewBar) {
                return;
            }

            const prevFilesStateCallback = viewBar.filesStateCallback;
            let refreshing = false;

            viewBar.events.addEventHandler('afterUpload', function (owner, eventArgs) {
                if (eventArgs && eventArgs.fileName) {
                    reset();
                }
            });

            // Render file after uploading
            viewBar.filesStateCallback = function (filesCount) {
                if (prevFilesStateCallback) {
                    prevFilesStateCallback(filesCount);
                }

                let allowedFilesControl = px_all[allowFilesId];
                if (allowedFilesControl) {
                    const allowedFiles = allowedFilesControl.getValue();

                    if (allowedFiles) {
                        primaryForm.allowedFiles = allowedFiles;
                    }
                }

                let allowedFilesMsgControl = px_all[allowFilesMsgId];
                if (allowedFilesMsgControl) {
                    const allowedFilesMsg = allowedFilesMsgControl.getValue();

                    if (allowedFilesMsg) {
                        primaryForm.allowedFilesMsg = allowedFilesMsg;
                    }
                }

                if (!filesCount || prevFilesCount === filesCount) {
                    return;
                }

                let allowUploadFileControl = px_all[allowUploadFileId];
                if (allowUploadFileControl) {
                    let allowUpload = allowUploadFileControl.getValue();

                    setEnabledFileAttach(allowUpload);
                }

                if (refreshing === true) {
                    return;
                }

                refreshing = true;

                setTimeout(function () {
                    form.refresh();
                    prevFilesCount = filesCount;
                    refreshing = false;
                }, 100);
            }
        }

        function setEnabledFileAttach(enabled) {
            let filesMenuBtn = null;

            for (var i = 0; i < viewBar.items.length; i++) {
                if (viewBar.items[i].commandName == "FilesMenuShow") {
                    filesMenuBtn = viewBar.items[i];
                    break;
                }
            }

            if (filesMenuBtn) {
                filesMenuBtn.setEnabled(enabled);
            }

            primaryForm.canAddFiles = enabled == true ? 2 : 0;
        }

        function captureDetailsGrid(grid) {
            detailsGrid = grid;
        }

        function commandPerformed(ds, e) {
            if (e.command.toLowerCase() === 'insert' ||
                e.command.toLowerCase() === 'delete' ||
                e.command.toLowerCase() === 'save') {

                return;
            }

            // Reset boxes
            if (e.command.toLowerCase() === 'cancel') {
                renderBoundingBoxes(true);

                return;
            }

            if (e.command.toLowerCase() === 'processrecognition') {
                isDataProcessed = false;

                const enableFile = ds.longRunMessage && ds.longRunCompleted === null && ds.longRunInProcess === null;
                setEnabledFileAttach(enableFile);

                return;
            }

            hideSynchronizationControls();

            if (ds.longRunCompleted === null && ds.longRunAborted === null) {
                return;
            }

            if (isDataProcessed === true) {
                return;
            }

            isDataProcessed = true;

            setEnabledFileAttach(false);
            renderBoundingBoxes(false);
        }

        function renderBoundingBoxes(isReloading) {
            const recognizedDataControl = px_all[recognizedDataId];
            if (!recognizedDataControl) {
                return;
            }

            let recognizedDataEncodedJson = recognizedDataControl.getValue();
            if (!recognizedDataEncodedJson) {
                return;
            }

            recognizedDataEncodedJson = recognizedDataEncodedJson.replace(/\+/g, '%20');

            const recognizedDataJson = decodeURIComponent(recognizedDataEncodedJson);
            if (!recognizedDataJson) {
                return;
            }

            let vendorTermData = null;
            const vendorTermControl = px_all[vendorTermId];
            if (vendorTermControl) {
                let vendorTermEncodedJson = vendorTermControl.getValue();

                if (vendorTermEncodedJson) {
                    vendorTermEncodedJson = vendorTermEncodedJson.replace(/\+/g, '%20');
                    const vendorTermJson = decodeURIComponent(vendorTermEncodedJson);

                    if (vendorTermJson) {
                        vendorTermData = JSON.parse(vendorTermJson);
                    }
                }
            }

            const recognizedData = JSON.parse(recognizedDataJson);
            pdfRecognitionViewer.processRecognizedData(recognizedData, vendorTermData, isReloading);
        }

        function hideSynchronizationControls() {
            const recognizedDataElement = document.getElementById(recognizedDataId);
            if (recognizedDataElement) {
                recognizedDataElement.style.display = 'none';
            }

            const vendorTermElement = document.getElementById(vendorTermId);
            if (vendorTermElement) {
                vendorTermElement.style.display = 'none';
            }

            const feedbackElement = document.getElementById(fieldBoundFeedbackId);
            if (feedbackElement) {
                feedbackElement.style.display = 'none';
            }
        }

        function renderPdf() {
            let fileId = null;

            let fileIdControl = px_all[fileIDControlID];
            if (fileIdControl) {
                fileId = fileIdControl.getValue();
            }

            if (fileId !== prevFileId) {
                reset();
            }
            else {
                return;
            }

            if (isPdfRendered === true) {
                return;
            }

            let fieldBoundFeedbackControl = px_all[fieldBoundFeedbackId];
            if (!fieldBoundFeedbackControl) {
                return;
            }

            const tableRelatedFeedbackControl = px_all[tableRelatedFeedbackId];
            if (!tableRelatedFeedbackControl) {
                return;
            }

            if (!fileId) {
                return;
            }

            prevFileId = fileId;

            let pdfUrl = '../../Frames/GetFile.ashx?inmemory=1&fileID=' + fileId;

            // Loaded via <script> tag, create shortcut to access PDF.js exports.
            let pdfjsLib = window['pdfjs-dist/build/pdf'];

            // The workerSrc property shall be specified.
            pdfjsLib.GlobalWorkerOptions.workerSrc = '../../Scripts/documentRecognition/pdf.js/pdf.worker.min.js';

            enableDragNDropContainer(false);

            let pdfRecognitionContainer = getPdfRecognitionContainer();
            const dumpTableFeedbackCallback = function () {
                datasource.executeCommand('DumpTableFeedback');
            };
            pdfRecognitionViewer = new PdfRecognitionViewer(pdfUrl, pdfjsLib, pdfRecognitionContainer,
                fieldBoundFeedbackControl, tableRelatedFeedbackControl, dumpTableFeedbackCallback,
                'Document.VendorID', 'RecognizedPONumber', 'PONumberJson'
            );

            pdfRecognitionViewer.trackFormControls(primaryForm);
            pdfRecognitionViewer.trackGridControls(detailsGrid);

            const callback = function () {
                if (isDataProcessed === true) {
                    return;
                }

                isDataProcessed = true;
                renderBoundingBoxes(false);
            }

            pdfRecognitionViewer.renderPdf(callback);

            isPdfRendered = true;
        }

        function getPdfRecognitionContainer() {
            return document.getElementById('pdfRecognitionViewerContainer');
        }

        function reset() {
            pdfRecognitionViewer = null;
            isPdfRendered = false;
            isDataProcessed = false;
            prevFilesCount = null;

            setEnabledFileAttach(true);

            let pdfRecognitionContainer = getPdfRecognitionContainer();
            while (pdfRecognitionContainer.firstChild) {
                pdfRecognitionContainer.removeChild(pdfRecognitionContainer.firstChild);
            }

            enableDragNDropContainer(true);
        }

        function enableDragNDropContainer(enable) {
            const container = document.getElementById('dragNdropContainer');
            if (!container) {
                return;
            }

            container.style.display = enable ? 'flex' : 'none';
        }
    </script>
</asp:Content>
