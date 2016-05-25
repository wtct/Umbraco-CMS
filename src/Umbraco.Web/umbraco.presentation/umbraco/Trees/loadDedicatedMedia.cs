using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using umbraco.BasePages;
using umbraco.businesslogic;
using umbraco.BusinessLogic.Actions;
using umbraco.cms.businesslogic;
using umbraco.cms.presentation.Trees;
using umbraco.interfaces;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace umbraco
{
    [Tree(Constants.Applications.Media, "dedicatedMedia", "Dedicated Media")]
    public class loadDedicatedMedia : BaseMediaTree
    {
        private int _StartNodeID;
        /// <summary>
		/// Create the linkable data types list and add the DataTypeUploadField guid to it.
		/// By default, any media type that is to be "linkable" in the WYSIWYG editor must contain
		/// a DataTypeUploadField data type which will ouput the value for the link, however, if 
		/// a developer wants the WYSIWYG editor to link to a custom media type, they will either have
		/// to create their own media tree and inherit from this one and override the GetLinkValue 
		/// or add another GUID to the LinkableMediaDataType list on application startup that matches
		/// the GUID of a custom data type. The order of GUIDs will determine the output value.
		/// </summary>
		static loadDedicatedMedia()
		{
			LinkableMediaDataTypes = new List<Guid>();
			LinkableMediaDataTypes.Add(new Guid(Constants.PropertyEditors.UploadField));
		}

		public loadDedicatedMedia(string application)
			: base(application)
		{
            int umbPageId = GetUmbPageIdByUrlReferrer();

            if (umbPageId != -1)
                _StartNodeID = GetDedicatedMediaFolderIdByContentNodeId(umbPageId);
            else
                _StartNodeID = CurrentUser.StartMediaId;
		}		

        protected override void CreateRootNode(ref XmlTreeNode rootNode)
        {            

            if (this.IsDialog)
                rootNode.Action = "javascript:openMedia(-1);";
            else
                rootNode.Action = "javascript:" + ClientTools.Scripts.OpenDashboard("Media");
        }

        protected override void CreateRootNodeActions(ref List<IAction> actions)
		{
			actions.Clear();
			actions.Add(ActionNew.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionSort.Instance);
			actions.Add(ContextMenuSeperator.Instance);
			actions.Add(ActionRefresh.Instance);
		}

        protected override void CreateAllowedActions(ref List<IAction> actions)
        {
            actions.Clear();
            actions.Add(ActionNew.Instance);
            actions.Add(ActionMove.Instance);
            actions.Add(ActionDelete.Instance);
            actions.Add(ActionSort.Instance);
            actions.Add(ContextMenuSeperator.Instance);
            actions.Add(ActionRefresh.Instance);
        }

		/// <summary>
		/// If the user is an admin, always return entire tree structure, otherwise
		/// return the user's start node id.
		/// </summary>
		public override int StartNodeID
		{
            get
            {
                return _StartNodeID;
            }
		}

        private int GetUmbPageIdByUrlReferrer()
        {
            Uri urlReferrer = HttpContext.Current.Request.UrlReferrer;

            if (urlReferrer != null)
            {
                if (!string.IsNullOrEmpty(urlReferrer.Query))
                {
                    var queryString = HttpUtility.ParseQueryString(urlReferrer.Query);

                    if (queryString.ContainsKey("umbPageId"))
                    {
                        int umbPageId = -1;

                        string sUmbPageId = queryString["umbPageId"];
                        if (Int32.TryParse(sUmbPageId, out umbPageId))
                            return umbPageId;
                    }
                }
            }

            return -1;
        }

        private int GetDedicatedMediaFolderIdByContentNodeId(int contentNodeId)
        {
            IContent contentNode = ApplicationContext.Current.Services.ContentService.GetById(contentNodeId);

            List<string> NodeNames = new List<string>();

            NodeNames.Add(contentNode.Name);

            while (contentNode.Level > 1)
            {
                contentNode = ApplicationContext.Current.Services.ContentService.GetById(contentNode.ParentId);
                NodeNames.Add(contentNode.Name);
            }

            IMedia mediaRootNode = ApplicationContext.Current.Services.MediaService.GetRootMedia().Where(m => string.Equals(m.Name, NodeNames[NodeNames.Count - 1], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (mediaRootNode != null)
            {
                IMedia currentMediaNode = mediaRootNode;

                foreach (string nodeName in NodeNames.Reverse<string>().Skip(1))
                {
                    IMedia childMediaNode = currentMediaNode.Children().Where(m => string.Equals(m.Name, nodeName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                    if (childMediaNode == null)
                        return -1;

                    currentMediaNode = childMediaNode;
                }

                return currentMediaNode.Id;
            }

            return -1;
        }
    }
}
