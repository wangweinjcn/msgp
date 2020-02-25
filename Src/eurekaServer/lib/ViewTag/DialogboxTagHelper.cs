   #if (NETCORE || NETSTANDARD2_0 )
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;

namespace FrmLib.web
{
    [HtmlTargetElement("dialogbox")]
    public class DialogboxTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string style = output.Attributes.Where(a => a.Name == "style").FirstOrDefault()?.Value.ToString() ?? null;

            output.Attributes.RemoveAll("style");

            output.TagName = "div";

            output.PreContent.AppendLine();
            output.PreContent.AppendText("    <div data-bind=\"display:IsShow()\" class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"myModalLabel\" aria-hidden=\"true\" data-backdrop=\"false\" data-keyboard=\"true\">");
            output.PreContent.AppendLine();

            output.PreContent.AppendText("        <div class=\"modal-dialog\" style=\"{0}\">", style ?? "");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("            <div class=\"modal-content\" style=\"{0}\">", style ?? "");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                <div class=\"modal-header\">");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                    <label data-bind=\"text:Title\"></label>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                    <button type=\"button\" class=\"close\" data-dismiss=\"modal\" data-bind=\"click:function(){ IsShow(false);}\"><span aria-hidden=\"true\">&times;</span></button>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                </div>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                <div class=\"modal-body\">");
            output.PreContent.AppendLine();


            output.PostContent.AppendLine();
            output.PostContent.AppendText("               </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("               <div class=\"modal-footer\">");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("                    <button type=\"button\" class=\"btn btn-default\" data-bind=\"click:Save\">保存</button>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("                    <button type=\"button\" class=\"btn btn-default\" data-dismiss=\"modal\" data-bind=\"click:function(){ IsShow(false);}\">关闭</button>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("               </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("           </div>");
            output.PostContent.AppendLine();

            output.PostContent.AppendText("       </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("   </div>");
            output.PostContent.AppendLine();


        }
    }
    [HtmlTargetElement("dialogboxnobutton")]
    public class DialogboxNoButtonTagHelper : TagHelper
    {
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            string style = output.Attributes.Where(a => a.Name == "style").FirstOrDefault()?.Value.ToString() ?? null;

            output.Attributes.RemoveAll("style");

            output.TagName = "div";

            output.PreContent.AppendLine();
            output.PreContent.AppendText("    <div data-bind=\"display:IsShow()\" class=\"modal fade\" tabindex=\"-1\" role=\"dialog\" aria-labelledby=\"myModalLabel\" aria-hidden=\"true\" data-backdrop=\"false\" data-keyboard=\"true\">");
            output.PreContent.AppendLine();

            output.PreContent.AppendText("        <div class=\"modal-dialog\" style=\"{0}\">", style ?? "");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("            <div class=\"modal-content\" style=\"{0}\">", style ?? "");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                <div class=\"modal-header\">");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                    <label data-bind=\"text:Title\"></label>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                    <button type=\"button\" class=\"close\" data-dismiss=\"modal\" data-bind=\"click:function(){ IsShow(false);}\"><span aria-hidden=\"true\">&times;</span></button>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                </div>");
            output.PreContent.AppendLine();
            output.PreContent.AppendText("                <div class=\"modal-body\">");
            output.PreContent.AppendLine();


            output.PostContent.AppendLine();
            output.PostContent.AppendText("               </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("               <div class=\"modal-footer\">");
            output.PostContent.AppendText("               </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("           </div>");
            output.PostContent.AppendLine();

            output.PostContent.AppendText("       </div>");
            output.PostContent.AppendLine();
            output.PostContent.AppendText("   </div>");
            output.PostContent.AppendLine();


        }
    }

    public static class TagHelperContentExtension
    {
        public static TagHelperContent AppendText(this TagHelperContent content, string text)
        {
            content.AppendHtml(text);
            return content;
        }
        public static TagHelperContent AppendText(this TagHelperContent content, string format, params object[] args)
        {
            content.AppendFormat(format, args);
            return content;
        }
        public static TagHelperContent AppendLine(this TagHelperContent content)
        {
            content.AppendHtml(Environment.NewLine);
            return content;
        }
    }
}
#endif