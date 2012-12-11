namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class AddedReadMessagesTable : DbMigration
	{
		public override void Up()
		{
			CreateTable("ReadMessages",
						c => new
						{
							Key = c.Int(nullable: false, identity: true),
							UserId = c.String(nullable: false),
							MessageId = c.String(nullable: false),
						}).PrimaryKey(t => t.Key);
		}

		public override void Down()
		{
			DropTable("ReadMessages");
		}
	}
}
