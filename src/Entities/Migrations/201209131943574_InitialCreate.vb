Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Partial Public Class InitialCreate
        Inherits DbMigration

        Public Overrides Sub Up()
			CreateTable(
				"dbo.ActivitySamples",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.sampleTimeAndDate = c.DateTimeOffset(nullable:=False),
						.inputActivity_keyboardActivity = c.Int(nullable:=False),
						.inputActivity_mouseActivity = c.Int(nullable:=False),
						.activityWindowDetail_id = c.Int()
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ActivityWindowDetails", Function(t) t.activityWindowDetail_id)

			CreateTable(
				"dbo.ActivityWindowDetails",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.windowText = c.String(nullable:=False),
						.windowDetailUsageCount = c.Int(nullable:=False),
						.processInformation_id = c.Int(nullable:=False),
						.windowManuallyAssignedClass_id = c.Int(),
						.windowPredictedClass_id = c.Int()
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.Processes", Function(t) t.processInformation_id, cascadeDelete:=True) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.windowManuallyAssignedClass_id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.windowPredictedClass_id)

            CreateTable(
                "dbo.Processes",
                Function(c) New With
                    {
                        .id = c.Int(nullable:=False, identity:=True),
                        .processName = c.String(nullable:=False),
                        .processUsageCount = c.Int(nullable:=False)
                    }) _
                .PrimaryKey(Function(t) t.id)

            CreateTable(
                "dbo.ClassificationClasses",
                Function(c) New With
                    {
                        .id = c.Int(nullable:=False, identity:=True),
                        .className = c.String(nullable:=False),
                        .classColor_background = c.String(nullable:=False),
                        .classColor_foreground = c.String(nullable:=False),
                        .classifier_id = c.Int(nullable:=False)
                    }) _
                .PrimaryKey(Function(t) t.id) _
                .ForeignKey("dbo.Classifiers", Function(t) t.classifier_id, cascadeDelete:=True) _

            CreateTable(
                "dbo.Classifiers",
                Function(c) New With
                    {
                        .id = c.Int(nullable:=False, identity:=True),
                        .classifierName = c.String(nullable:=False)
                    }) _
                .PrimaryKey(Function(t) t.id)

        End Sub

        Public Overrides Sub Down()
            DropIndex("dbo.ClassificationClasses", New String() {"classifier_id"})
            DropIndex("dbo.ActivityWindowDetails", New String() {"windowPredictedClass_id"})
            DropIndex("dbo.ActivityWindowDetails", New String() {"windowManuallyAssignedClass_id"})
            DropIndex("dbo.ActivityWindowDetails", New String() {"processInformation_id"})
            DropIndex("dbo.ActivitySamples", New String() {"activityWindowDetail_id"})
            DropForeignKey("dbo.ClassificationClasses", "classifier_id", "dbo.Classifiers")
            DropForeignKey("dbo.ActivityWindowDetails", "windowPredictedClass_id", "dbo.ClassificationClasses")
            DropForeignKey("dbo.ActivityWindowDetails", "windowManuallyAssignedClass_id", "dbo.ClassificationClasses")
            DropForeignKey("dbo.ActivityWindowDetails", "processInformation_id", "dbo.Processes")
            DropForeignKey("dbo.ActivitySamples", "activityWindowDetail_id", "dbo.ActivityWindowDetails")
            DropTable("dbo.Classifiers")
            DropTable("dbo.ClassificationClasses")
            DropTable("dbo.Processes")
            DropTable("dbo.ActivityWindowDetails")
            DropTable("dbo.ActivitySamples")
        End Sub
    End Class
End Namespace
