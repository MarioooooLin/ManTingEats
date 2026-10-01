-- MySQL dump 10.13  Distrib 8.0.46, for Linux (x86_64)
--
-- Host: localhost    Database: ManTingEatsDb
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Dumping data for table `MenuItems`
--

LOCK TABLES `MenuItems` WRITE;
/*!40000 ALTER TABLE `MenuItems` DISABLE KEYS */;
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (1,'菜1',0,300.00,1,0,1);
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (2,'喝1',1,1000.00,1,0,0);
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (3,'菜2',0,90.00,1,1,1);
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (4,'喝2',1,20.00,1,0,0);
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (5,'油漆',2,999.00,1,0,0);
INSERT  IGNORE INTO `MenuItems` (`Id`, `Name`, `Category`, `Price`, `IsActive`, `SupportsAddOns`, `SupportsSpiceLevel`) VALUES (6,'鍋子',2,666.00,0,0,0);
/*!40000 ALTER TABLE `MenuItems` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `AddOns`
--

LOCK TABLES `AddOns` WRITE;
/*!40000 ALTER TABLE `AddOns` DISABLE KEYS */;
INSERT  IGNORE INTO `AddOns` (`Id`, `Name`, `Price`, `IsActive`) VALUES (1,'蛋蛋',10.00,1);
INSERT  IGNORE INTO `AddOns` (`Id`, `Name`, `Price`, `IsActive`) VALUES (2,'瓜瓜',200.00,1);
/*!40000 ALTER TABLE `AddOns` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed
