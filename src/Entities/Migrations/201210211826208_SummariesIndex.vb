Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class SummariesIndex
        Inherits DbMigration
    
        Public Overrides Sub Up()
            CreateIndex("dbo.IntegralSummaries", "summaryType")
            CreateIndex("dbo.IntegralSummaries", "summaryTimeAndDate")
        End Sub
        
        Public Overrides Sub Down()
            DropIndex("dbo.IntegralSummaries", New String() {"summaryType"})
            DropIndex("dbo.IntegralSummaries", New String() {"summaryTimeAndDate"})
        End Sub
    End Class
End Namespace
