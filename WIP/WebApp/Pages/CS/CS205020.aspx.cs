using PX.CS;
using PX.Data;
using PX.Web.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using PX.Objects.Common;
using System.Collections.Specialized;

public partial class Page_CS205020 : PX.Web.UI.PXPage
{
	public PXSelect<CSScreenAttribute, Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
		And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>,
		And<CSScreenAttribute.typeValue, Equal<Required<CSScreenAttribute.typeValue>>>>>> udfTypedAttribute;
	public PXSelect<CSScreenAttribute, Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
		And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>>>> udfAttributes;

	PXDropDown newDD = null;
	PXSelector newSel = null;
	PXFieldState selState = null;

	protected override void OnPreLoad(EventArgs e)
	{
		base.OnPreLoad(e);
		// we need this here in order to generate grid columns
		if (selState != null) (newSel as IFieldEditor).SynchronizeState(selState);
	}

	protected override void OnInitComplete(EventArgs e)
	{
		base.OnInitComplete(e);

		form.ControlsCreating += Form_ControlsCreating;
		form.DataBinding += Form_DataBinding;
		//string screen = Request.QueryString["forscreen"];
		var prms = Session[PX.CS.CSAttributeMaint2.SessionKey] as PX.CS.CSAttributeMaint2.ControlParams;
		var bndPrms = prms as PX.CS.CSAttributeMaint2.BoundParams;

		System.Web.UI.WebControls.WebControl templateContainer = null;
		if (form.Items.Count > 0)
			templateContainer = form.Items[1].TemplateContainer;
		if (templateContainer != null)
		{
			newDD = ControlHelper.FindControl(templateContainer, "newDD") as PXDropDown;
			newSel = ControlHelper.FindControl(templateContainer, "newSel") as PXSelector;
		}

		var dd = prms as PX.CS.CSAttributeMaint2.ComboBoxParams;
		var sel = prms as PX.CS.CSAttributeMaint2.SelectorParams;
		var cache = ds.DataGraph.Views["ScreenSettings"].Cache;
		var scrn = cache.Current as PX.Data.AttribParams;

		if (scrn != null)
		{
			var oldScrnID = scrn.ScreenID;
			if (null != prms && !string.IsNullOrEmpty(prms.ScreenId))
			{
				cache.SetValue<PX.Data.AttribParams.screenID>(cache.Current, prms.ScreenId);
				Session[sessionKey] = prms.ScreenId;
			}
			else if (string.IsNullOrEmpty(scrn.ScreenID))
			{
				cache.SetValue<PX.Data.AttribParams.screenID>(cache.Current, Session[sessionKey]);
			}
			if (scrn.ScreenID != oldScrnID) scrn.TypeName = string.Empty;
		}

		if (null != bndPrms && !string.IsNullOrEmpty(bndPrms.UDFTypeField))
		{

			string viewName = PX.CS.CSAttributeMaint2.CreateViewName(prms.ScreenId, bndPrms.ViewName);
			var siteMapNode = PXSiteMap.Provider.FindSiteMapNodeByScreenID(prms.ScreenId);
			var type = System.Web.Compilation.BuildManager.GetType(siteMapNode.GraphType, false);
			var graph = PXGraph.CreateInstance(type);
			udfTypedAttribute = new PXSelect<CSScreenAttribute,
				Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
				And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>,
				And<CSScreenAttribute.typeValue, Equal<Required<CSScreenAttribute.typeValue>>>>>>
				(graph);
			udfAttributes = new PXSelect<CSScreenAttribute,
				Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
				And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>>>>
				(graph);
			var dataSource = GetDefaultDataSource(this) as PXDataSource;

			var view = new PXView(dataSource.DataGraph, true, BqlCommand.CreateInstance(typeof(Select<>), graph.GetItemType(bndPrms.ViewName)), new PXSelectDelegate(delegate ()
			{
				return Enumerable.Empty<object>();
			}));

			dataSource.DataGraph.Views.Add(viewName, view);

			if (null != dd)
			{
				newDD.DataMember = viewName;
				newDD.Size = "XXL";
				newDD.Hidden = false;
				//newDD.DataField = "TypeName";
				newSel.DataField = string.Empty;

				//newDD.ApplyStyleSheetSkin(this);
				var state = dataSource.DataGraph.Views[viewName].Cache.GetStateExt(null, bndPrms.UDFTypeField) as PXStringState;
				state.ValueLabelDic["<All>"] = "ALL";
				if (!string.IsNullOrEmpty(bndPrms.ViewName))
					state.DisplayName = bndPrms.ViewName;
				(newDD as IFieldEditor).SynchronizeState(state);
				newDD.Items.Insert(0, new PXListItem("<All>", "ALL"));
				newDD.ValueChanged += (object sender, EventArgs e1) =>
				{
					var dDown = sender as PXDropDown;
					if (scrn != null && dDown != null)
						scrn.TypeName = dDown.Value as string;
				};
				newDD.DataBind();
			}
			else
			{
				newSel.DataMember = viewName;
				newSel.Hidden = false;
				newDD.DataField = string.Empty;

				newSel.TextChanged += (object sender, EventArgs e1) =>
				{
					var selector = sender as PXSelector;
					if (scrn != null && selector != null)
						scrn.TypeName = selector.Value as string;
				};
				var state = dataSource.DataGraph.Views[viewName].Cache.GetStateExt(null, bndPrms.UDFTypeField) as PXFieldState;
				(newSel as IFieldEditor).SynchronizeState(this.selState = state);
				newSel.Value = scrn.TypeName;
				newSel.DataBind();
			}
		}
		else if(null != prms)
		{
			var siteMapNode = PXSiteMap.Provider.FindSiteMapNodeByScreenID(prms.ScreenId);
			var type = System.Web.Compilation.BuildManager.GetType(siteMapNode.GraphType, false);
			var graph = PXGraph.CreateInstance(type);
			udfTypedAttribute = new PXSelect<CSScreenAttribute,
					Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
					And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>,
					And<CSScreenAttribute.typeValue, Equal<Required<CSScreenAttribute.typeValue>>>>>>
					(graph);
			udfAttributes = new PXSelect<CSScreenAttribute,
				Where<CSScreenAttribute.screenID, Equal<Required<CSScreenAttribute.screenID>>,
				And<CSScreenAttribute.attributeID, Equal<Required<CSScreenAttribute.attributeID>>>>>
				(graph);
		}
		var visibility = ControlHelper.FindControl(form.Items[1].TemplateContainer, "visibility") as PXGrid;
		var visibilityItems = visibility.Levels.Items.FirstOrDefault().Columns.Items;
		visibilityItems.FirstOrDefault(n => n.DataField == "Name").Header.Text = PXLocalizer.Localize(PX.Data.UserDefinedFieldsMessages.UserDefinedAttribute, typeof(PX.Data.UserDefinedFieldsMessages).FullName);
		visibilityItems.FirstOrDefault(n => n.DataField == "Required").Header.Text = PXLocalizer.Localize(PX.Data.UserDefinedFieldsMessages.Required, typeof(PX.Data.UserDefinedFieldsMessages).FullName);
		visibilityItems.FirstOrDefault(n => n.DataField == "Hidden").Header.Text = PXLocalizer.Localize(PX.Data.UserDefinedFieldsMessages.Hidden, typeof(PX.Data.UserDefinedFieldsMessages).FullName);
		visibilityItems.FirstOrDefault(n => n.DataField == "DefaultValue").Header.Text = PXLocalizer.Localize(PX.Data.UserDefinedFieldsMessages.Default, typeof(PX.Data.UserDefinedFieldsMessages).FullName);
		if (ControlHelper.IsCallbackOwner(visibility) && ControlHelper.GetCommandName(this)=="Refresh")
		{
			Form_DataBinding(this, EventArgs.Empty);
		}
	}

	private void Form_DataBinding(object sender, EventArgs e)
	{
		var cache = ds.DataGraph.Views["ScreenSettings"].Cache;
		var scrn = cache.Current as PX.Data.AttribParams;
		if (scrn.ScreenID != null || PXGraph.GeneratorIsActive)
		{
			string screen = PXGraph.GeneratorIsActive ? "00000000" : scrn.ScreenID.Replace(".", "");
			if (form.Items.Count > 1)
			{
				var visibility = ControlHelper.FindControl(form.Items[1].TemplateContainer, "visibility") as PXGrid;
				if (null != visibility)
				{
					string ddVal = null;
					if (null != newDD)
					{
						if (Context != null && Context.Request != null && Context.Request.Form != null )
						{
							ddVal = Context.Request.Form[newDD.ClientID.Replace("_", "$") + "$text"];
						}
						if (!string.IsNullOrEmpty(ddVal))
						{
							if ("<All>" == ddVal) scrn.TypeName = string.Empty;
						}

					}
					if (newDD != null && newDD.SelectedValue != null)
						visibility.DataSource = PX.Web.UI.PXAttribPanel.GetVisibility(screen, newDD.SelectedValue);
					else if (newSel != null && newSel.Text != null)
						visibility.DataSource = PX.Web.UI.PXAttribPanel.GetVisibility(screen, newSel.Text);

					visibility.Columns["Hidden"].DataType = TypeCode.Boolean;
					visibility.Columns["Required"].DataType = TypeCode.Boolean;
					visibility.Columns["DefaultValue"].DataType = TypeCode.String;
					visibility.DataBind();
					//visibility.ApplyStyleSheetSkin(this);
				}
			}
		}
	}

	const string sessionKey = "LastAttribScreen";
	private void Form_ControlsCreating(object sender, System.ComponentModel.CancelEventArgs e)
	{
		var cache = ds.DataGraph.Views["ScreenSettings"].Cache;
		var scrn = cache.Current as PX.Data.AttribParams;
		if (scrn.ScreenID != null || PXGraph.GeneratorIsActive)
		{
			string screen = PXGraph.GeneratorIsActive ? "00000000" : scrn.ScreenID.Replace(".", "");
			var controls = new List<Control>();
			var panel = new PXAttribPanel() { ID = "atPanel" };
			panel.Controls.Add(PXAttribPanel.CreateDesignAttribTable(screen, controls, this));
			var ctrls = form.Items[0].TemplateContainer.Controls;
			bool found = false;
			foreach (Control c in ctrls)
			{
				if (c.ID == panel.ID && c is PXAttribPanel)
				{
					panel = c as PXAttribPanel;
					found = true;
					break;
				}
			}
			if (!found)
				form.Items[0].TemplateContainer.Controls.Add(panel);

			var node = PXSiteMap.Provider.FindSiteMapNodeByScreenID(screen);
			if (null != node) panel.ScreenTitle = node.Title;

			if (form.Items.Count > 1)
			{
				var visibility = ControlHelper.FindControl(form.Items[1].TemplateContainer, "visibility") as PXGrid;
				if (null != visibility)
				{
					//visibility.DataSource = PX.Web.UI.PXAttribPanel.GetVisibility(screen, newDD?.SelectedValue ?? newSel?.Text);
					visibility.Columns["Hidden"].DataType = TypeCode.Boolean;
					visibility.Columns["Required"].DataType = TypeCode.Boolean;
					visibility.Columns["DefaultValue"].DataType = TypeCode.String;
					visibility.RefetchRow += Visibility_RefetchRow;
					visibility.RowUpdating += Visibility_RowUpdating;
				}
			}
		}
	}

	private void UpdateVisibility(CSScreenAttribute attribute, bool hidden, bool required, string defaultValue)
	{
		attribute.Hidden = hidden;
		attribute.Required = required;
		attribute.DefaultValue = defaultValue;
		udfTypedAttribute.Cache.Update(attribute);
		udfTypedAttribute.Cache.Persist(PXDBOperation.Update);
	}

	private void UpdateVisibility(IOrderedDictionary newValues)
	{
		var cache = ds.DataGraph.Views["ScreenSettings"].Cache;
		var scrn = cache.Current as PX.Data.AttribParams;
		string attributeID = newValues["AttributeID"].ToString().ToUpper(), screenID = scrn.ScreenID;
		//string typeValue = newDD?.SelectedValue ?? newSel?.Text;
		string typeValue = string.Empty;
		if (string.IsNullOrEmpty(scrn.TypeName)) typeValue = string.Empty;
		else
		{
			typeValue = scrn.TypeName;
		}

		if (string.Equals(typeValue, "All", StringComparison.CurrentCultureIgnoreCase))
			typeValue = string.Empty;
		bool hidden = Convert.ToBoolean(newValues["Hidden"]), required = Convert.ToBoolean(newValues["Required"]);
		string defaultValue = Convert.ToString(newValues["DefaultValue"]);
		var attribute = udfTypedAttribute?.SelectSingle(screenID, attributeID, typeValue);
		if (hidden)
			newValues["Required"] = required = false;

		if (attribute == null)
		{
			var commonAttribute = udfTypedAttribute?.SelectSingle(screenID, attributeID, string.Empty);
			attribute = (CSScreenAttribute)udfTypedAttribute?.Cache.Insert();
			attribute.ScreenID = screenID;
			attribute.AttributeID = attributeID;
			attribute.TypeValue = typeValue;
			attribute.Hidden = hidden;
			attribute.Required = !hidden ? required : false;
			attribute.Column = commonAttribute.Column;
			attribute.Row = commonAttribute.Row;
			attribute.DefaultValue = defaultValue;
			udfTypedAttribute.Cache.Persist(PXDBOperation.Insert);
		}
		else if (attribute.Hidden != hidden || attribute.Required != required || attribute.DefaultValue != defaultValue)
		{

			if (!string.IsNullOrEmpty(typeValue))
				UpdateVisibility(attribute, hidden, required, defaultValue);
			else
			{
				var itemsToDelete = udfAttributes.Select(screenID, attributeID)
					.Select(attr => attr.GetItem<CSScreenAttribute>())
					.Where(attr => !string.IsNullOrEmpty(attr.TypeValue));
				if (!itemsToDelete.Any())
					UpdateVisibility(attribute, hidden, required, defaultValue);
				else
					try
					{
						if (((CSAttributeMaint2)ds.DataGraph).ScreenSettings.Ask(Messages.Warning,
								Messages.UDFVisibilityChanging,
								MessageButtons.YesNo, MessageIcon.Warning) == WebDialogResult.Yes)
						{
							UpdateVisibility(attribute, hidden, required, defaultValue);

							foreach (var attr in itemsToDelete)
							{
								udfAttributes.Cache.PersistDeleted(attr);
							}
						}
					}
					catch (PXDialogRequiredException ex)
					{
						ex.DataSourceID = ds.ID;
						throw;
					}
			}
		}
	}

	private void Visibility_RowUpdating(object sender, PXDBUpdateEventArgs e)
	{
		UpdateVisibility(e.NewValues);
	}


	private void Visibility_RefetchRow(object sender, PXDBUpdateEventArgs e)
	{
		UpdateVisibility(e.NewValues);
	}

	protected void visibility_RowDataBound(object sender, PXGridRowEventArgs e)
	{
		var state = (e.Row.DataItem as VisibilityInfo).State;
		switch (state.DataType.Name)
		{
			case "Boolean":
				e.Row.Cells["DefaultValue"].DataType = TypeCode.Boolean;
				break;
			case "DateTime":
				e.Row.Cells["DefaultValue"].DataType = TypeCode.DateTime;
				break;
			case "String":
				e.Row.Cells["DefaultValue"].DataType = TypeCode.String;
				var sState = ((PXStringState)state);
				if (sState.ValueLabelDic != null)
				{
					foreach (var item in sState.ValueLabelDic)
						e.Row.Cells["DefaultValue"].ValueItems.Items.Add(new PXValueItem(item.Key, item.Value));
					e.Row.Cells["DefaultValue"].ValueItems.MultiSelect = sState.MultiSelect;
					e.Row.Cells["DefaultValue"].ValueField = sState.ValueField;
				}
				break;
			case "Object":
				e.Row.Cells["DefaultValue"].DataType = TypeCode.Object;
				var selState = state as PXSelectorState;
				if (selState != null)
				{
					var graph = (sender as PXGrid).DataGraph;
					graph = udfTypedAttribute.View.Graph;
					var type = System.Web.Compilation.PXBuildManager.GetType(selState.SchemaObject, true); 
					if (graph != null)
					{
						var cache = graph.Caches[type];
						var state1 = cache.GetStateExt(null, selState.SchemaField) as PXStringState;
						e.Row.Cells["DefaultValue"].ViewName = state1.ViewName;
						e.Row.Cells["DefaultValue"].ValueField = selState.SchemaField;
					}
					else
					{
						e.Row.Cells["DefaultValue"].ViewName = selState.SchemaObject;
						e.Row.Cells["DefaultValue"].ValueField = selState.SchemaField;
					}
				}
				break;
		}
	}

	protected void visibility_SyncCellState(object sender, PXSyncCellStateEventArgs e)
	{
		var cache = ds.DataGraph.Views["ScreenSettings"].Cache;
		var scrn = cache.Current as PX.Data.AttribParams;
		PXSelectorState udfAttrState = null;
		if (scrn.ScreenID != null || PXGraph.GeneratorIsActive)
		{
			string screen = PXGraph.GeneratorIsActive ? "00000000" : scrn.ScreenID.Replace(".", "");

			List<VisibilityInfo> l = null;
			if (newDD != null && newDD.SelectedValue != null)
			{
				l = PX.Web.UI.PXAttribPanel.GetVisibility(screen, newDD.SelectedValue);
			}
			else if (newSel != null && newSel.Text != null)
			{
				l = PX.Web.UI.PXAttribPanel.GetVisibility(screen, newSel.Text);
			}
			if (l == null) return;
			udfAttrState = l.Where(n => n.AttributeID == e.RowValues.Value.ToString()).Select(n => n.State).FirstOrDefault() as PXSelectorState;
			if (udfAttrState == null) return;
		}

		var graph = udfTypedAttribute.View.Graph;
		var type = System.Web.Compilation.PXBuildManager.GetType(udfAttrState.SchemaObject, true); //"PX.Objects.CM.CurrencyList"
		cache = graph.Caches[type];
		var state1 = cache.GetStateExt(null, udfAttrState.SchemaField) as PXStringState;

		e.State = state1;
	}

}
