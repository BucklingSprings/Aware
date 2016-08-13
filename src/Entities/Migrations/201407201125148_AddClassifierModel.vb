Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddClassifierModel
        Inherits DbMigration
    
        Public Overrides Sub Up()
			CreateTable(
				"dbo.ClassifierModels",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.createdOn = c.DateTimeOffset(nullable:=False)
					}) _
				.PrimaryKey(Function(t) t.id)

			CreateTable(
				"dbo.ClassifierModelExamples",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.trained = c.Int(nullable:=False),
						.model_id = c.Int(nullable:=False),
						.windowDetail_id = c.Int(nullable:=False)
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassifierModels", Function(t) t.model_id, cascadeDelete:=True) _
				.ForeignKey("dbo.ActivityWindowDetails", Function(t) t.windowDetail_id, cascadeDelete:=True)

		End Sub

		Public Overrides Sub Down()
			DropIndex("dbo.ClassifierModelExamples", New String() {"windowDetail_id"})
			DropIndex("dbo.ClassifierModelExamples", New String() {"model_id"})
			DropForeignKey("dbo.ClassifierModelExamples", "windowDetail_id", "dbo.ActivityWindowDetails")
			DropForeignKey("dbo.ClassifierModelExamples", "model_id", "dbo.ClassifierModels")
			DropTable("dbo.ClassifierModelExamples")
			DropTable("dbo.ClassifierModels")
		End Sub
    End Class
End Namespace
