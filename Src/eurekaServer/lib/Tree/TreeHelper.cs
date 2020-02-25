using Ace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ace.Web.Mvc.Common.Tree
{
    public static class TreeHelper
    {
        public static string ConvertToJson(List<TreeSelectModel> data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(ConvertToJson(data, null, ""));
            sb.Append("]");
            return sb.ToString();
        }
        public static string ConvertToJson(this List<TreeViewModel> data, string parentId = null)
        {
            StringBuilder strJson = new StringBuilder();
            List<TreeViewModel> item = data.FindAll(t => t.ParentId == parentId);
            strJson.Append("[");
            if (item.Count > 0)
            {
                foreach (TreeViewModel entity in item)
                {
                    strJson.Append("{");
                    strJson.Append("\"id\":\"" + entity.Id + "\",");
                    strJson.Append("\"text\":\"" + entity.Text.Replace("&nbsp;", "") + "\",");
                    strJson.Append("\"value\":\"" + entity.Value + "\",");
                    if (entity.Title != null && !string.IsNullOrEmpty(entity.Title.Replace("&nbsp;", "")))
                    {
                        strJson.Append("\"title\":\"" + entity.Title.Replace("&nbsp;", "") + "\",");
                    }
                    if (entity.Img != null && !string.IsNullOrEmpty(entity.Img.Replace("&nbsp;", "")))
                    {
                        strJson.Append("\"img\":\"" + entity.Img.Replace("&nbsp;", "") + "\",");
                    }
                    if (entity.Checkstate != null)
                    {
                        strJson.Append("\"checkstate\":" + entity.Checkstate + ",");
                    }
                    if (entity.ParentId != null)
                    {
                        strJson.Append("\"parentnodes\":\"" + entity.ParentId + "\",");
                    }
                    strJson.Append("\"showcheck\":" + entity.Showcheck.ToString().ToLower() + ",");
                    strJson.Append("\"isexpand\":" + entity.Isexpand.ToString().ToLower() + ",");
                    if (entity.Complete == true)
                    {
                        strJson.Append("\"complete\":" + entity.Complete.ToString().ToLower() + ",");
                    }
                    strJson.Append("\"hasChildren\":" + entity.HasChildren.ToString().ToLower() + ",");
                    strJson.Append("\"ChildNodes\":" + ConvertToJson(data, entity.Id) + "");
                    strJson.Append("},");
                }
                strJson = strJson.Remove(strJson.Length - 1, 1);
            }
            strJson.Append("]");
            return strJson.ToString();
        }

        public static List<TreeModel> TreeWhere<TreeModel>(List<TreeModel> entityList, Predicate<TreeModel> condition, Func<TreeModel, bool> isRoot, Func<TreeModel, TreeModel, bool> isParentNodeOf)
        {
            List<TreeModel> locateList = entityList.FindAll(condition);
            List<TreeModel> treeList = new List<TreeModel>();
            foreach (TreeModel entity in locateList)
            {
                treeList.Add(entity);
                TreeModel currentNode = entity;
                while (true)
                {
                    if (isRoot(currentNode)) /* currentNode.ParentId == null */
                        break;

                    TreeModel upRecord = entityList.Find(a => isParentNodeOf(a, currentNode));
                    if (upRecord != null)
                    {
                        treeList.Add(upRecord);
                        currentNode = upRecord;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return treeList.Distinct().ToList();
        }
        public static List<TreeModel> TreeWhere<TreeModel>(List<TreeModel> entityList, Predicate<TreeModel> condition, Func<TreeModel, string> idSelector, Func<TreeModel, string> parentIdSelector)
        {
            List<TreeModel> locateList = entityList.FindAll(condition);
            List<TreeModel> treeList = new List<TreeModel>();
            foreach (TreeModel entity in locateList)
            {
                treeList.Add(entity);
                TreeModel currentNode = entity;
                while (true)
                {
                    string parentId = parentIdSelector(currentNode);
                    if (parentId == null) /* currentNode.ParentId == null */
                        break;

                    TreeModel upRecord = entityList.Find(a => idSelector(a) == parentId);
                    if (upRecord != null)
                    {
                        treeList.Add(upRecord);
                        currentNode = upRecord;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return treeList.Distinct().ToList();
        }


        static string ConvertToJson(List<TreeSelectModel> data, string parentId, string blank)
        {
            StringBuilder sb = new StringBuilder();
            var ChildNodeList = data.FindAll(t => t.parentId == parentId);
            var tabline = "";
            if (parentId != null)
            {
                tabline = "　　";
            }
            if (ChildNodeList.Count > 0)
            {
                tabline = tabline + blank;
            }
            foreach (TreeSelectModel entity in ChildNodeList)
            {
                entity.text = tabline + entity.text;
                string strJson = JsonHelper.Serialize(entity);
                sb.Append(strJson);
                sb.Append(ConvertToJson(data, entity.id, tabline));
            }
            return sb.ToString().Replace("}{", "},{");
        }
    }
}