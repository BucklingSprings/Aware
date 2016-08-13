Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class SwitchToGenericClassificationScheme
        Inherits DbMigration
    
        Public Overrides Sub Up()
            DropForeignKey("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id", "dbo.ClassificationClasses")
            DropForeignKey("dbo.ActivityWindowDetails", "windowPredictedClass_id", "dbo.ClassificationClasses")
            DropIndex("dbo.ActivityWindowDetails", New String() { "windowManuallyAssignedClass_id" })
            DropIndex("dbo.ActivityWindowDetails", New String() { "windowPredictedClass_id" })
			CreateTable(
				"dbo.ActivityWindowDetailClassAssignments",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.classifierDefinitionVersion = c.Int(nullable:=False),
						.classifierIdentifier = c.Int(nullable:=False),
						.assignedClass_id = c.Int(),
						.ActivityWindowDetail_id = c.Int(nullable:=False)
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.assignedClass_id) _
				.ForeignKey("dbo.ActivityWindowDetails", Function(t) t.ActivityWindowDetail_id)

			CreateTable(
				"dbo.CategoryClassifierPhrases",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.phrase = c.String(nullable:=False),
						.order = c.Int(nullable:=False),
						.classificationClass_id = c.Int(nullable:=False)
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.classificationClass_id, cascadeDelete:=True)

            AddColumn("dbo.ClassificationClasses", "catchAll", Function(c) c.Boolean(nullable:=False))
            AddColumn("dbo.Classifiers", "classifierType", Function(c) c.String(nullable:=False))
            AddColumn("dbo.Classifiers", "limitToMostUsed", Function(c) c.Boolean(nullable:=False))
            AddColumn("dbo.Classifiers", "classifierDefinitionVersion", Function(c) c.Int(nullable:=False))
            AddColumn("dbo.Classifiers", "classLimit", Function(c) c.Int(nullable:=False))
            DropColumn("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id")
            DropColumn("dbo.ActivityWindowDetails", "windowPredictedClass_id")
            DropColumn("dbo.ClassificationClasses", "classColor_background")
            DropColumn("dbo.ClassificationClasses", "classColor_foreground")
        End Sub

        Public Overrides Sub Down()
            AddColumn("dbo.ClassificationClasses", "classColor_foreground", Function(c) c.String(nullable:=False))
            AddColumn("dbo.ClassificationClasses", "classColor_background", Function(c) c.String(nullable:=False))
            AddColumn("dbo.ActivityWindowDetails", "windowPredictedClass_id", Function(c) c.Int())
            AddColumn("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id", Function(c) c.Int())
            DropIndex("dbo.CategoryClassifierPhrases", New String() {"classificationClass_id"})
            DropIndex("dbo.ActivityWindowDetailClassAssignments", New String() {"ActivityWindowDetail_id"})
            DropIndex("dbo.ActivityWindowDetailClassAssignments", New String() {"assignedClass_id"})
            DropForeignKey("dbo.CategoryClassifierPhrases", "classificationClass_id", "dbo.ClassificationClasses")
            DropForeignKey("dbo.ActivityWindowDetailClassAssignments", "ActivityWindowDetail_id", "dbo.ActivityWindowDetails")
            DropForeignKey("dbo.ActivityWindowDetailClassAssignments", "assignedClass_id", "dbo.ClassificationClasses")
            DropColumn("dbo.Classifiers", "classLimit")
            DropColumn("dbo.Classifiers", "classifierDefinitionVersion")
            DropColumn("dbo.Classifiers", "limitToMostUsed")
            DropColumn("dbo.Classifiers", "classifierType")
            DropColumn("dbo.ClassificationClasses", "catchAll")
            DropTable("dbo.CategoryClassifierPhrases")
            DropTable("dbo.ActivityWindowDetailClassAssignments")
            CreateIndex("dbo.ActivityWindowDetails", "windowPredictedClass_id")
            CreateIndex("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id")
            AddForeignKey("dbo.ActivityWindowDetails", "windowPredictedClass_id", "dbo.ClassificationClasses", "id")
            AddForeignKey("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id", "dbo.ClassificationClasses", "id")
        End Sub
    End Class
End Namespace
