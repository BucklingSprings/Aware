Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddSoftDelete
        Inherits DbMigration
    
        Public Overrides Sub Up()
            AddColumn("dbo.ClassificationClasses", "deleted", Function(c) c.Boolean(nullable := False))
        End Sub
        
        Public Overrides Sub Down()
            DropColumn("dbo.ClassificationClasses", "deleted")
        End Sub
    End Class
End Namespace
