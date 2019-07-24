using System;

namespace NadekoBot.Core.Services.Database.Models
{
    public class WaifuItem : DbEntity
    {
        public int? WaifuInfoId { get; set; }
        public string ItemEmoji { get; set; }
        public int Price { get; set; }
        public ItemName Item { get; set; }

        public enum ItemName
        {
            Banana,
            Tea,
            Coffee,
            Chocolate,
            Pizza,
            Cake,
            Medal,
            Rooster,
            Surprise,
            Clover,
            Book,
            LoveLetter,
            Spider,
            Snake,
            Horse,
            Moneybag,
            Mask,
            Ring,
            Dagger,
            Shield,
            Swords,
            Alien,
            Unicorn,
            Diamond,
            Crown,
            Castle,
            Dragon,
            /*Manga,
            Guitar,
            Kimono,
            Iphone,
            Laptop,
            Honor,
            Newcomer,
            Star,
            Moon,
            Love,*/
        }

        public WaifuItem()
        {

        }

        public WaifuItem(string itemEmoji, int price, ItemName item)
        {
            ItemEmoji = itemEmoji;
            Price = price;
            Item = item;
        }

        public static WaifuItem GetItemObject(ItemName itemName, int mult)
        {
            WaifuItem wi;
            switch (itemName)
            {
                case ItemName.Banana:
                    wi = new WaifuItem("🍌", 10, itemName);
                    break;
                case ItemName.Tea:
                    wi = new WaifuItem("🍵", 50, itemName);
                    break;
                case ItemName.Coffee:
                    wi = new WaifuItem("☕", 50, itemName);
                    break;
                case ItemName.Chocolate:
                    wi = new WaifuItem("🍫", 100, itemName);
                    break;
                case ItemName.Pizza:
                    wi = new WaifuItem("🍕", 150, itemName);
                    break;
                case ItemName.Cake:
                    wi = new WaifuItem("🍰", 200, itemName);
                    break;
                case ItemName.Medal:
                    wi = new WaifuItem("🏅", 250, itemName);
                    break;
                case ItemName.Rooster:
                    wi = new WaifuItem("🐓", 300, itemName);
                    break;
                case ItemName.Surprise:
                    wi = new WaifuItem("🎁", 400, itemName);
                    break;
                case ItemName.Clover:
                    wi = new WaifuItem("🍀", 500, itemName);
                    break;
                case ItemName.Book:
                    wi = new WaifuItem("📖", 550, itemName);
                    break;
                case ItemName.LoveLetter:
                    wi = new WaifuItem("💌", 600, itemName);
                    break;
                case ItemName.Spider:
                    wi = new WaifuItem("🕷️", 700, itemName);
                    break;
                case ItemName.Snake:
                    wi = new WaifuItem("🐍", 800, itemName);
                    break;
                case ItemName.Horse:
                    wi = new WaifuItem("🐎", 900, itemName);
                    break;
                case ItemName.Moneybag:
                    wi = new WaifuItem("💰", 1000, itemName);
                    break;
                case ItemName.Mask:
                    wi = new WaifuItem("👹", 1500, itemName);
                    break;
                case ItemName.Ring:
                    wi = new WaifuItem("💍", 1700, itemName);
                    break;
                case ItemName.Dagger:
                    wi = new WaifuItem("🗡️", 2500, itemName);
                    break;
                case ItemName.Shield:
                    wi = new WaifuItem("🛡️", 2500, itemName);
                    break;
                case ItemName.Swords:
                    wi = new WaifuItem("⚔️", 5000, itemName);
                    break;
                case ItemName.Alien:
                    wi = new WaifuItem("👾", 9000, itemName);
                    break;
                case ItemName.Unicorn:
                    wi = new WaifuItem("🦄", 10000, itemName);
                    break;
                case ItemName.Diamond:
                    wi = new WaifuItem("💎", 15000, itemName);
                    break;
                case ItemName.Crown:
                    wi = new WaifuItem("👑", 25000, itemName);
                    break;
                case ItemName.Castle:
                    wi = new WaifuItem("🏰", 50000, itemName);
                    break;
                case ItemName.Dragon:
                    wi = new WaifuItem("🐲", 99999, itemName);
                    break;
                /*case ItemName.Manga:
                    wi = new WaifuItem("📓", 800, itemName);
                    break;
                case ItemName.Guitar:
                    wi = new WaifuItem("🎸", 5000, itemName);
                    break;
                case ItemName.Iphone:
                    wi = new WaifuItem("📱", 8000, itemName);
                    break;
                case ItemName.Laptop:
                    wi = new WaifuItem("💻", 10000, itemName);
                    break;
                case ItemName.Honor:
                    wi = new WaifuItem("🏅", 15000, itemName);
                    break;
                case ItemName.Newcomer:
                    wi = new WaifuItem("👾", 50000, itemName);
                    break;
                case ItemName.Star:
                    wi = new WaifuItem("🌟", 99999, itemName);
                    break;
                case ItemName.Moon:
                    wi = new WaifuItem("🌕", 100000, itemName);
                    break;
                case ItemName.Love:
                    wi = new WaifuItem("💝", 200000, itemName);
                    break;*/
                default:
                    throw new ArgumentException("Item is not implemented", nameof(itemName));
            }
            wi.Price = wi.Price * mult;
            return wi;
        }
    }
}


/*
🍪 Cookie 10
🌹  Rose 50
💌 Love Letter 100
🍫  Chocolate 200
🍚 Rice 400
🎟  Movie Ticket 800
📔 Book 1.5k
💄  Lipstick 3k
💻 Laptop 5k
🎻 Violin 7.5k
💍 Ring 10k
*/
