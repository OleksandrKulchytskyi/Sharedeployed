namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class DeleteReadMessagesTable : DbMigration
	{
		public override void Up()
		{
			DropTable("ReadMessages");
		}

		public override void Down()
		{
			CreateTable(
				"ReadMessages",
				c => new
					{
						Key = c.Int(nullable: false, identity: true),
						UserId = c.String(nullable: false),
						MessageId = c.String(nullable: false)
					})
				.PrimaryKey(t => t.Key);

		}
	}
}
