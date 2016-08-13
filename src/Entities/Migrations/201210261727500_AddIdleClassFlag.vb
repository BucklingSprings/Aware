Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddIdleClassFlag
        Inherits DbMigration
    
        Public Overrides Sub Up()
            AddColumn("dbo.ClassificationClasses", "idle", Function(c) c.Boolean(nullable := False))
        End Sub
        
        Public Overrides Sub Down()
            DropColumn("dbo.ClassificationClasses", "idle")
        End Sub
    End Class
End Namespace
