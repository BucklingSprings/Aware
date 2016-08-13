Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class CombineSamples
        Inherits DbMigration
    
        Public Overrides Sub Up()
            AddColumn("dbo.ActivitySamples", "sampleStartTimeAndDate", Function(c) c.DateTimeOffset(nullable := False))
            AddColumn("dbo.ActivitySamples", "sampleEndTimeAndDate", Function(c) c.DateTimeOffset(nullable := False))
            DropColumn("dbo.ActivitySamples", "sampleTimeAndDate")
            DropColumn("dbo.ActivityWindowDetails", "windowDetailUsageCount")
            DropColumn("dbo.Processes", "processUsageCount")
        End Sub
        
        Public Overrides Sub Down()
            AddColumn("dbo.Processes", "processUsageCount", Function(c) c.Int(nullable := False))
            AddColumn("dbo.ActivityWindowDetails", "windowDetailUsageCount", Function(c) c.Int(nullable := False))
            AddColumn("dbo.ActivitySamples", "sampleTimeAndDate", Function(c) c.DateTimeOffset(nullable := False))
            DropColumn("dbo.ActivitySamples", "sampleEndTimeAndDate")
            DropColumn("dbo.ActivitySamples", "sampleStartTimeAndDate")
        End Sub
    End Class
End Namespace
