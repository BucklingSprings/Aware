Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddGoals
        Inherits DbMigration
    
        Public Overrides Sub Up()
			CreateTable(
				"dbo.StoredGoals",
				Function(c) New With
					{
						.id = c.Int(nullable:=False, identity:=True),
						.value = c.Int(nullable:=False),
						.comparison = c.String(nullable:=False),
						.period = c.String(nullable:=False),
						.target = c.String(nullable:=False),
						.startTime = c.DateTimeOffset(nullable:=False),
						.endTime = c.DateTimeOffset(nullable:=False),
						.goalClass_id = c.Int()
					}) _
				.PrimaryKey(Function(t) t.id) _
				.ForeignKey("dbo.ClassificationClasses", Function(t) t.goalClass_id, )
            
        End Sub
        
        Public Overrides Sub Down()
            DropIndex("dbo.StoredGoals", New String() { "goalClass_id" })
            DropForeignKey("dbo.StoredGoals", "goalClass_id", "dbo.ClassificationClasses")
            DropTable("dbo.StoredGoals")
        End Sub
    End Class
End Namespace
