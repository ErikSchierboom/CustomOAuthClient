namespace CustomOAuthClient.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Web.Mvc;

    /// <summary>
    /// Extensions to the <see cref="HtmlHelper"/> class. These extensions all work on IDictionary string, string instances.
    /// </summary>
    public static class HtmlHelperExtensions
    {
        public static MvcHtmlString HiddenFor<TModel>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, IDictionary<string, string>>> expression)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var value = (IDictionary<string, string>)metadata.Model;
            var expressionText = ExpressionHelper.GetExpressionText(expression);

            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expressionText);
            
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("Field name is null or empty.");
            }

            var strings = new List<string>(value.Count * 2);

            // For every element in the dictionary, we will create an input field for the key and one for the value
            for (var i = 0; i < value.Count; i++)
            {
                strings.Add(InputHelperForKey(htmlHelper, metadata, value, expressionText, null, fullName, i));
                strings.Add(InputHelperForValue(htmlHelper, metadata, value, expressionText, null, fullName, i));
            }

            return new MvcHtmlString(string.Join("\n", strings));
        }

        private static string InputHelperForKey(HtmlHelper htmlHelper, ModelMetadata metadata, IDictionary<string, string> value, string expression, IDictionary<string, object> htmlAttributes, string fullName, int index)
        {
            return InputTagHelper(htmlHelper, metadata, expression, htmlAttributes, fullName, index, "Key", value.Keys.ElementAt(index));
        }

        private static string InputHelperForValue(HtmlHelper htmlHelper, ModelMetadata metadata, IDictionary<string, string> value, string expression, IDictionary<string, object> htmlAttributes, string fullName, int index)
        {
            return InputTagHelper(htmlHelper, metadata, expression, htmlAttributes, fullName, index, "Value", value.Values.ElementAt(index));
        }

        private static string InputTagHelper(HtmlHelper htmlHelper, ModelMetadata metadata, string expression, IDictionary<string, object> htmlAttributes, string fullName, int index, string fieldType, string val)
        {
            // Create the input tag with the specified parameters
            var tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("type", HtmlHelper.GetInputTypeString(InputType.Hidden));
            tagBuilder.MergeAttribute("value", val);

            // The name is the most important part of the tag building, as using the correct name
            // will ensure that the default model binder will correctly be able to deserialize the dictionary
            tagBuilder.MergeAttribute("name", string.Format("{0}[{1}].{2}", fullName, index, fieldType), true);
            
            tagBuilder.GenerateId(fullName);

            // If there are any errors for the named field, we add the css attribute.
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState))
            {
                if (modelState.Errors.Count > 0)
                {
                    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
                }
            }

            // Merge the unobtrusive validation attributes (if there are any)
            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(expression, metadata));

            return tagBuilder.ToString(TagRenderMode.SelfClosing);
        }
    }
}