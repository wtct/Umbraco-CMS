using System;
using System.Data;
using System.Linq;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.Rdbms;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Umbraco.Core.Persistence.Migrations.Upgrades.TargetVersionSeven
{
    [Migration("7.0.0", 8, Constants.System.UmbracoMigrationName)]
    public class AlterTagRelationsTable : MigrationBase
    {
        public AlterTagRelationsTable(ISqlSyntaxProvider sqlSyntax, ILogger logger) : base(sqlSyntax, logger)
        {
        }

        public override void Up()
        {
            if (Context == null || Context.Database == null) return;

            Initial();

            Upgrade();

            Final();
        }

        private void Initial()
        {
            var constraints = SqlSyntax.GetConstraintsPerColumn(Context.Database).Distinct().ToArray();

            //create a new col which we will make a foreign key, but first needs to be populated with data.
            Alter.Table("cmsTagRelationship").AddColumn("propertyTypeId").AsInt32().Nullable();

            //drop the foreign key on umbracoNode.  Must drop foreign key first before primary key can be removed in MySql.
            if (Context.CurrentDatabaseProvider == DatabaseProviders.MySql)
            {
                Delete.ForeignKey().FromTable("cmsTagRelationship").ForeignColumn("nodeId").ToTable("umbracoNode").PrimaryColumn("id");
                //check for another strange really old one that might have existed
                if (constraints.Any(x => x.Item1 == "cmsTagRelationship" && x.Item2 == "tagId"))
                {
                    Delete.ForeignKey().FromTable("cmsTagRelationship").ForeignColumn("tagId").ToTable("cmsTags").PrimaryColumn("id");
                }
            }
            else
            {
                //Before we try to delete this constraint, we'll see if it exists first, some older schemas never had it and some older schema's had this named
                // differently than the default.

                var constraintMatches = constraints.Where(x => x.Item1 == "cmsTagRelationship" && x.Item2 == "nodeId" && x.Item3.InvariantStartsWith("PK_") == false);

                foreach (var constraint in constraintMatches)
                {
                    Delete.ForeignKey(constraint.Item3).OnTable("cmsTagRelationship");
                }
            }

            //we need to drop the primary key, this is sql specific since MySQL has never had primary keys on this table
            // at least since 6.0 and the new installation way but perhaps it had them way back in 4.x so we need to check
            // it exists before trying to drop it.
            if (Context.CurrentDatabaseProvider == DatabaseProviders.MySql)
            {   
                //this will let us know if this pk exists on this table
                if (constraints.Count(x => x.Item1.InvariantEquals("cmsTagRelationship") && x.Item3.InvariantEquals("PRIMARY")) > 0)
                {
                    Delete.PrimaryKey("PK_cmsTagRelationship").FromTable("cmsTagRelationship");
                }
            }
            else
            {
                //lookup the PK by name
                var pkName = constraints.FirstOrDefault(x => x.Item1.InvariantEquals("cmsTagRelationship") && x.Item3.InvariantStartsWith("PK_"));
                if (pkName != null)
                {
                    Delete.PrimaryKey(pkName.Item3).FromTable("cmsTagRelationship");    
                }
            }
            
        }

        private void Upgrade()
        {
            int deletedRows = Context.Database.Execute(@"DELETE TR
                FROM cmsTagRelationship TR
                WHERE NOT EXISTS (SELECT DISTINCT cmsTagRelationship.nodeId as NodeId, cmsTags.id as TagId
                    FROM cmsTags 
                    JOIN cmsTagRelationship ON cmsTagRelationship.tagId = cmsTags.id
                    JOIN umbracoNode ON umbracoNode.id = cmsTagRelationship.nodeId
                    JOIN cmsContent ON cmsContent.nodeId = umbracoNode.id
                    JOIN cmsContentType ON cmsContentType.nodeId = cmsContent.contentType
                    JOIN cmsPropertyType ON cmsPropertyType.contentTypeId = cmsContentType.nodeId
                    JOIN cmsDataType ON cmsDataType.nodeId = cmsPropertyType.dataTypeId
                    JOIN cmsDataTypePreValues ON cmsDataTypePreValues.dataTypeNodeId = cmsDataType.nodeId
                    WHERE cmsDataType.controlId = '4023E540-92F5-11DD-AD8B-0800200C9A66' 
					    AND cmsDataTypePreValues.Alias = 'group' AND cmsTags.[Group] = cmsDataTypePreValues.[Value]
					    AND cmsTagRelationship.nodeId = TR.NodeId 
					    AND cmsTags.id = TR.TagId)");

            Logger.Warn<AlterTagRelationsTable>($"There was no cmsContent reference for {deletedRows} rows in cmsTagRelationship. " +
                "The new tag system only supports tags with references to content in the cmsContent and cmsPropertyType tables.");

            int updatedRows = Context.Database.Execute(@"UPDATE TR
                SET TR.PropertyTypeId = (SELECT cmsPropertyType.id as PropertyTypeId
                    FROM cmsTags 
                    JOIN cmsTagRelationship ON cmsTagRelationship.tagId = cmsTags.id
                    JOIN umbracoNode ON umbracoNode.id = cmsTagRelationship.nodeId
                    JOIN cmsContent ON cmsContent.nodeId = umbracoNode.id
                    JOIN cmsContentType ON cmsContentType.nodeId = cmsContent.contentType
                    JOIN cmsPropertyType ON cmsPropertyType.contentTypeId = cmsContentType.nodeId
                    JOIN cmsDataType ON cmsDataType.nodeId = cmsPropertyType.dataTypeId
                    JOIN cmsDataTypePreValues ON cmsDataTypePreValues.dataTypeNodeId = cmsDataType.nodeId
                    WHERE cmsDataType.controlId = '4023E540-92F5-11DD-AD8B-0800200C9A66' 
					    AND cmsDataTypePreValues.Alias = 'group' AND cmsTags.[Group] = cmsDataTypePreValues.[Value] 
					    AND cmsTagRelationship.NodeId = TR.NodeId
					    AND cmsTagRelationship.TagId = TR.TagId)
                FROM cmsTagRelationship TR ");

            Logger.Info<AlterTagRelationsTable>($"Updated {updatedRows} rows in cmsTagRelationship with proper propertyTypeId.");
        }

        private void Final()
        {
            //we need to change this to not nullable
            Alter.Table("cmsTagRelationship").AlterColumn("propertyTypeId").AsInt32().NotNullable();

            //we need to re-add the new primary key on all 3 columns
            Create.PrimaryKey("PK_cmsTagRelationship").OnTable("cmsTagRelationship").Columns(new[] { "nodeId", "propertyTypeId", "tagId" });

            //now we need to add a foreign key to the propertyTypeId column and change it's constraints
            Create.ForeignKey("FK_cmsTagRelationship_cmsPropertyType")
                  .FromTable("cmsTagRelationship")
                  .ForeignColumn("propertyTypeId")
                  .ToTable("cmsPropertyType")
                  .PrimaryColumn("id")
                  .OnDelete(Rule.None)
                  .OnUpdate(Rule.None);

            //now we need to add a foreign key to the nodeId column to cmsContent (intead of the original umbracoNode)
            Create.ForeignKey("FK_cmsTagRelationship_cmsContent")
                  .FromTable("cmsTagRelationship")
                  .ForeignColumn("nodeId")
                  .ToTable("cmsContent")
                  .PrimaryColumn("nodeId")
                  .OnDelete(Rule.None)
                  .OnUpdate(Rule.None);
        }

        public override void Down()
        {
            throw new DataLossException("Cannot downgrade from a version 7 database to a prior version, the database schema has already been modified");
        }
    }
}