Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class MoveClassAssignmentToSample
        Inherits DbMigration
    
        Public Overrides Sub Up()
            DropForeignKey("dbo.ActivityWindowDetailClassAssignments", "assignedClass_id", "dbo.ClassificationClasses")
            DropForeignKey("dbo.ActivityWindowDetailClassAssignments", "ActivityWindowDetail_id", "dbo.ActivityWindowDetails")
            DropIndex("dbo.ActivityWindowDetailClassAssignments", New String() { "assignedClass_id" })
            DropIndex("dbo.ActivityWindowDetailClassAssignments", New String() { "ActivityWindowDetail_id" })
			CreateTable(
				"dbo.SampleClassAssignments",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.classifierDefinitionVersion = c.Int(nullable:=False),
						.classifierIdentifier = c.Int(nullable:=False),
						.assignedClass_id = c.Int(nullable:=False),
						.ActivitySample_id = c.Int(nullable:=False)
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.assignedClass_id, cascadeDelete:=True) _
				.ForeignKey("dbo.ActivitySamples", Function(t) t.ActivitySample_id)
            
            DropTable("dbo.ActivityWindowDetailClassAssignments")
        End Sub
        
        Public Overrides Sub Down()
            CreateTable(
                "dbo.ActivityWindowDetailClassAssignments",
                Function(c) New With
                    {
                        .id = c.Int(nullable := False, identity := True),
                        .classifierDefinitionVersion = c.Int(nullable := False),
                        .classifierIdentifier = c.Int(nullable := False),
                        .assignedClass_id = c.Int(),
                        .ActivityWindowDetail_id = c.Int(nullable := False)
                    }) _
                .PrimaryKey(Function(t) t.id)
            
            DropIndex("dbo.SampleClassAssignments", New String() { "ActivitySample_id" })
            DropIndex("dbo.SampleClassAssignments", New String() { "assignedClass_id" })
            DropForeignKey("dbo.SampleClassAssignments", "ActivitySample_id", "dbo.ActivitySamples")
            DropForeignKey("dbo.SampleClassAssignments", "assignedClass_id", "dbo.ClassificationClasses")
            DropTable("dbo.SampleClassAssignments")
            CreateIndex("dbo.ActivityWindowDetailClassAssignments", "ActivityWindowDetail_id")
            CreateIndex("dbo.ActivityWindowDetailClassAssignments", "assignedClass_id")
            AddForeignKey("dbo.ActivityWindowDetailClassAssignments", "ActivityWindowDetail_id", "dbo.ActivityWindowDetails", "id")
            AddForeignKey("dbo.ActivityWindowDetailClassAssignments", "assignedClass_id", "dbo.ClassificationClasses", "id")
        End Sub
    End Class
End Namespace
