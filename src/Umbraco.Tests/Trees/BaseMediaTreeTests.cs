using NUnit.Framework;
using umbraco.cms.presentation.Trees;

namespace Umbraco.Tests.Trees
{
    [TestFixture]
    public class BaseMediaTreeTests
    {

        [TearDown]
        public void TestTearDown()
        {
            BaseTree.AfterTreeRender -= EventHandler;
            BaseTree.BeforeTreeRender -= EventHandler;
        }

        private void EventHandler(object sender, TreeEventArgs treeEventArgs)
        {

        }

        public class MyOptimizedMediaTree : BaseMediaTree
        {
            public MyOptimizedMediaTree(string application)
                : base(application)
            {
            }

            protected override void CreateRootNode(ref XmlTreeNode rootNode)
            {

            }
        }
        

    }
}