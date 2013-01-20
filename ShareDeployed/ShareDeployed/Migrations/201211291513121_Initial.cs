namespace ShareDeployed.Migrations
{
	using System.Data.Entity.Migrations;

	public partial class Initial : DbMigration
	{
		public override void Up()
		{
			CreateTable(
				"UserProfile",
				c => new
					{
						UserId = c.Int(nullable: false, identity: true),
						UserName = c.String(),
						Email = c.String(),
					})
				.PrimaryKey(t => t.UserId);

			//DropTable("Revenues");
			//DropTable("Expenses");
			//DropTable("Users");
			//DropTable("Words");
		}

		public override void Down()
		{
			//CreateTable(
			//	"Words",
			//	c => new
			//		{
			//			Id = c.Int(nullable: false, identity: true),
			//			UserId = c.Int(nullable: false),
			//			ForeignWord = c.String(nullable: false),
			//			Translation = c.String(nullable: false),
			//			Complicated = c.Boolean(nullable: false),
			//		})
			//	.PrimaryKey(t => t.Id);

			//CreateTable(
			//	"Users",
			//	c => new
			//		{
			//			Id = c.Int(nullable: false, identity: true),
			//			Name = c.String(nullable: false),
			//			LogonName = c.String(),
			//		})
			//	.PrimaryKey(t => t.Id);

			//CreateTable(
			//	"Expenses",
			//	c => new
			//		{
			//			Id = c.Int(nullable: false, identity: true),
			//			UserId = c.Int(nullable: false),
			//			Name = c.String(nullable: false),
			//			Time = c.DateTime(nullable: false),
			//			Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
			//			IsRequired = c.Boolean(nullable: false),
			//			Description = c.String(),
			//		})
			//	.PrimaryKey(t => t.Id);

			//CreateTable(
			//	"Revenues",
			//	c => new
			//		{
			//			Id = c.Int(nullable: false, identity: true),
			//			UserId = c.Int(nullable: false),
			//			Name = c.String(nullable: false),
			//			Time = c.DateTime(nullable: false),
			//			Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
			//			Description = c.String(),
			//		})
			//	.PrimaryKey(t => t.Id);

			DropTable("UserProfile");
		}
	}
}