Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddGoalIndices
        Inherits DbMigration
    
        Public Overrides Sub Up()
            CreateIndex("dbo.StoredGoals", "startTime")
            CreateIndex("dbo.StoredGoals", "endTime")
        End Sub

        Public Overrides Sub Down()
            DropIndex("dbo.StoredGoals", New String() {"startTime"})
            DropIndex("dbo.StoredGoals", New String() {"endTime"})
        End Sub
    End Class
End Namespace
