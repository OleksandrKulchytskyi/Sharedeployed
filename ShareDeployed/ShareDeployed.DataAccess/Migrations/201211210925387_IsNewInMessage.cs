namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class IsNewInMessage : DbMigration
	{
		public override void Up()
		{
			AddColumn("Messages", "IsNew", c => c.Boolean(nullable: false));
		}

		public override void Down()
		{
			DropColumn("Messages", "IsNew");
		}
	}
}
