Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class Summaries
        Inherits DbMigration
    
        Public Overrides Sub Up()
            CreateTable(
				"dbo.IntegralSummaries",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.summaryType = c.String(nullable:=False, maxLength:=200),
						.summaryTimeAndDate = c.DateTimeOffset(nullable:=False),
						.summary = c.Int(nullable:=False),
						.summaryClassifierDefinitionVersion = c.Int(),
						.summaryClassifierIdentifier = c.Int(),
						.summaryClass_id = c.Int()
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.summaryClass_id)
            
            AddColumn("dbo.CategoryClassifierPhrases", "orderBy", Function(c) c.Int(nullable := False))
            DropColumn("dbo.CategoryClassifierPhrases", "order")
        End Sub
        
        Public Overrides Sub Down()
            AddColumn("dbo.CategoryClassifierPhrases", "order", Function(c) c.Int(nullable := False))
            DropIndex("dbo.IntegralSummaries", New String() { "summaryClass_id" })
            DropForeignKey("dbo.IntegralSummaries", "summaryClass_id", "dbo.ClassificationClasses")
            DropColumn("dbo.CategoryClassifierPhrases", "orderBy")
            DropTable("dbo.IntegralSummaries")
        End Sub
    End Class
End Namespace
