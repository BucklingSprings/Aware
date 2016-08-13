Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
	Partial Public Class StructuredStoredSummaries
		Inherits DbMigration

		Public Overrides Sub Up()
			DropForeignKey("dbo.IntegralSummaries", "summaryClass_id", "dbo.ClassificationClasses")
			DropIndex("dbo.IntegralSummaries", New String() {"summaryClass_id"})
			CreateTable(
				"dbo.StoredSummaries",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.summaryType = c.String(nullable:=False, maxLength:=200),
						.summaryTimeAndDate = c.DateTimeOffset(nullable:=False),
						.summary = c.String(nullable:=False),
						.summaryClassifierDefinitionVersion = c.Int(),
						.summaryClassifierIdentifier = c.Int(),
						.summaryClass_id = c.Int()
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.summaryClass_id)

			DropTable("dbo.IntegralSummaries")
		End Sub

		Public Overrides Sub Down()
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
				.PrimaryKey(Function(t) t.id)

			DropIndex("dbo.StoredSummaries", New String() {"summaryClass_id"})
			DropForeignKey("dbo.StoredSummaries", "summaryClass_id", "dbo.ClassificationClasses")
			DropTable("dbo.StoredSummaries")
			CreateIndex("dbo.IntegralSummaries", "summaryClass_id")
			AddForeignKey("dbo.IntegralSummaries", "summaryClass_id", "dbo.ClassificationClasses", "id")
		End Sub
	End Class
End Namespace
