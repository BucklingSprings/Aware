Imports System
Imports System.Data.Entity.Migrations

Namespace BucklingSprings.Aware.Entitities
    Public Partial Class AddConfigurations
        Inherits DbMigration
    
        Public Overrides Sub Up()
            CreateTable(
                "dbo.Configurations",
                Function(c) New With
                    {
                        .id = c.Int(nullable := False, identity := True),
                        .name = c.String(nullable := False),
                        .value = c.String(nullable := False)
                    }) _
                .PrimaryKey(Function(t) t.id)
            
        End Sub
        
        Public Overrides Sub Down()
            DropTable("dbo.Configurations")
        End Sub
    End Class
End Namespace
