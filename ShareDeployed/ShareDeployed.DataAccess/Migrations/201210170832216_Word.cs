namespace ShareDeployed.DataAccess.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class Word : DbMigration
	{
		public override void Up()
		{
			CreateTable("Words",
						c => new
							{
								Id = c.Int(nullable: false, identity: true),
								UserId = c.Int(nullable: false),
								ForeignWord = c.String(nullable: false),
								Translation = c.String(nullable: false),
								Complicated = c.Boolean(nullable: false),
							})
						.PrimaryKey(t => t.Id);

		}

		public override void Down()
		{
			DropTable("Words");
		}
	}
}
