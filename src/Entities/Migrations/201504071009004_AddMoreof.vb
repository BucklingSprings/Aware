Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddMoreof
        Inherits DbMigration
    
        Public Overrides Sub Up()
            AddColumn("dbo.ClassificationClasses", "moreOf", Function(c) c.Int(nullable := False))
        End Sub
        
        Public Overrides Sub Down()
            DropColumn("dbo.ClassificationClasses", "moreOf")
        End Sub
    End Class
End Namespace
