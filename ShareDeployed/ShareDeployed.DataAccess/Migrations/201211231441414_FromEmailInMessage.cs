namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class FromEmailInMessage : DbMigration
	{
		public override void Up()
		{
			AddColumn("Messages", "FromEmail", c => c.String());
		}

		public override void Down()
		{
			DropColumn("Messages", "FromEmail");
		}
	}
}
