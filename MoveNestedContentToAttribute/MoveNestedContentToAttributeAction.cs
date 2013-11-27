using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Xaml.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml.Parsing;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace MoveNestedContentToAttribute
{
    [ContextAction(Name = "Move text to attribute", Group = "XAML", Description = "Moves the text content to an attribute", Priority = 0)]
    public class MoveNestedContentToAttributeAction : ContextActionBase
    {
        private readonly XamlContextActionDataProvider _provider;

        private readonly Dictionary<string, string> _supportedTags = new Dictionary<string, string>
            {
                {"Label", "Content"},
                {"Button", "Content"},
                {"CheckBox", "Content"},
                {"RadioButton", "Content"},
                {"RepeatButton", "Content"},
                {"TextBlock", "Text"},
                {"TextBox", "Text"},
            }; 

        public MoveNestedContentToAttributeAction([NotNull] XamlContextActionDataProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException("provider");
            _provider = provider;
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var identifier = _provider.GetSelectedElement<IXmlFloatingTextTokenNode>(true, true);
            if (identifier == null)
                return false;
            if (!(identifier is XmlFloatingTextToken))
            {
                return false;
            }
            var containingNode = identifier.GetContainingNode<IXmlTag>();
            if (containingNode == null)
            {
                return false;
            }
            var tagName = containingNode.HeaderNode.Name.XmlName;
            if (!_supportedTags.ContainsKey(tagName))
            {
                return false;
            }
            return true;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var identifier = _provider.GetSelectedElement<IXmlFloatingTextTokenNode>(true, true);
            if (identifier == null)
                return null;
            if (!(identifier is XmlFloatingTextToken))
            {
                return null;
            }
            var containingNode = identifier.GetContainingNode<IXmlTag>();
            if (containingNode == null)
            {
                return null;
            }
            var tagName = containingNode.HeaderNode.Name.XmlName;
            var text = containingNode.InnerText.Trim();

            //add attribute
            var elementFactory = XmlElementFactory.GetInstance(containingNode);
            var attribute = elementFactory.CreateAttribute(string.Format("{0}=\"{1}\"", _supportedTags[tagName], text));
            containingNode.AddAttributeBefore(attribute, null);

            //remove the child text nodes
            ModificationUtil.DeleteChildRange(containingNode.InnerXml);

            //and make the tag empty if we can
            if (XmlTagUtil.CanBeEmptyTag(containingNode))
            {
                XmlTagUtil.MakeEmptyTag(containingNode);    
            }
            _provider.PsiServices.MarkAsDirty(_provider.SourceFile);

            return null;
        }

        public override string Text
        {
            get { return "Move text to attribute"; }
        }
    }
}
