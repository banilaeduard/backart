CREATE DATABASE `backart` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `AspNetRoles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `DataKeyLocation` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `locationCode` longtext CHARACTER SET utf8mb4 NULL,
	`townName` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_DataKeyLocation` PRIMARY KEY (`Id`),
    CONSTRAINT `AK_DataKeyLocation_name` UNIQUE (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUsers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `Address` longtext CHARACTER SET utf8mb4 NULL,
    `DataKeyLocationId` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Tenant` longtext CHARACTER SET utf8mb4 NULL,
    `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
    `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime(6) NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUsers_DataKeyLocation_DataKeyLocationId` FOREIGN KEY (`DataKeyLocationId`) REFERENCES `DataKeyLocation` (`name`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserLogins` (
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserRoles` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `AspNetUserTokens` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `RefreshToken` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Token` longtext CHARACTER SET utf8mb4 NULL,
    `Expires` datetime(6) NOT NULL,
    `Created` datetime(6) NOT NULL,
    `CreatedByIp` longtext CHARACTER SET utf8mb4 NULL,
    `Revoked` datetime(6) NULL,
    `RevokedByIp` longtext CHARACTER SET utf8mb4 NULL,
    `ReplacedByToken` longtext CHARACTER SET utf8mb4 NULL,
    `AppIdentityUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_RefreshToken` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RefreshToken_AspNetUsers_AppIdentityUserId` FOREIGN KEY (`AppIdentityUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);


CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);


CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);


CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);


CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);


CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);


CREATE INDEX `IX_AspNetUsers_DataKeyLocationId` ON `AspNetUsers` (`DataKeyLocationId`);


CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);


CREATE INDEX `IX_RefreshToken_AppIdentityUserId` ON `RefreshToken` (`AppIdentityUserId`);


ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `CodeAttribute` (
    `InnerValue` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Tag` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Id` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayValue` longtext CHARACTER SET utf8mb4 NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `Id` PRIMARY KEY (`Tag`, `InnerValue`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Codes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CodeDisplay` longtext CHARACTER SET utf8mb4 NULL,
    `CodeValue` longtext CHARACTER SET utf8mb4 NULL,
    `AttributeTags` longtext CHARACTER SET utf8mb4 NULL,
    `CodeValueFormat` longtext CHARACTER SET utf8mb4 NULL,
    `isRoot` tinyint(1) NOT NULL,
    `args` longtext CHARACTER SET utf8mb4 NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    `CodeLinkId` int NULL,
    CONSTRAINT `PK_Codes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Codes_Codes_CodeLinkId` FOREIGN KEY (`CodeLinkId`) REFERENCES `Codes` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_Codes_CodeLinkId` ON `Codes` (`CodeLinkId`);


ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `CodeAttributeSnapshot` (
    `InnerValue` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Tag` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Id` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayValue` longtext CHARACTER SET utf8mb4 NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `Id` PRIMARY KEY (`Tag`, `InnerValue`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Complaints` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    `Status` longtext CHARACTER SET utf8mb4 NULL,
    `NrComanda` longtext CHARACTER SET utf8mb4 NULL,
    `isDeleted` tinyint(1) NOT NULL,
    `DataKeyId` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Complaints` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Complaints_DataKeyLocation_DataKeyId` FOREIGN KEY (`DataKeyId`) REFERENCES `DataKeyLocation` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;


CREATE TABLE `Ticket` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CodeValue` longtext CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `HasAttachments` tinyint(1) NOT NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    `ComplaintId` int NOT NULL,
    `isDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Ticket` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Ticket_Complaints_ComplaintId` FOREIGN KEY (`ComplaintId`) REFERENCES `Complaints` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `Attachment` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Data` longtext CHARACTER SET utf8mb4 NULL,
    `Title` longtext CHARACTER SET utf8mb4 NULL,
    `TicketId` int NOT NULL,
    `Extension` longtext CHARACTER SET utf8mb4 NULL,
    `ContentType` longtext CHARACTER SET utf8mb4 NULL,
    `StorageType` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    CONSTRAINT `PK_Attachment` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Attachment_Ticket_TicketId` FOREIGN KEY (`TicketId`) REFERENCES `Ticket` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `CodeLinkSnapshot` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CodeDisplay` longtext CHARACTER SET utf8mb4 NULL,
    `CodeValue` longtext CHARACTER SET utf8mb4 NULL,
    `AttributeTags` longtext CHARACTER SET utf8mb4 NULL,
    `CodeValueFormat` longtext CHARACTER SET utf8mb4 NULL,
    `isRoot` tinyint(1) NOT NULL,
    `args` longtext CHARACTER SET utf8mb4 NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    `CodeLinkId` int NULL,
    `TicketId` int NULL,
    CONSTRAINT `PK_CodeLinkSnapshot` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CodeLinkSnapshot_CodeLinkSnapshot_CodeLinkId` FOREIGN KEY (`CodeLinkId`) REFERENCES `CodeLinkSnapshot` (`Id`),
    CONSTRAINT `FK_CodeLinkSnapshot_Ticket_TicketId` FOREIGN KEY (`TicketId`) REFERENCES `Ticket` (`Id`)
) CHARACTER SET=utf8mb4;


CREATE INDEX `IX_Attachment_TicketId` ON `Attachment` (`TicketId`);


CREATE INDEX `IX_CodeLinkSnapshot_CodeLinkId` ON `CodeLinkSnapshot` (`CodeLinkId`);


CREATE INDEX `IX_CodeLinkSnapshot_TicketId` ON `CodeLinkSnapshot` (`TicketId`);


CREATE INDEX `IX_Complaints_DataKeyId` ON `Complaints` (`DataKeyId`);


CREATE INDEX `IX_Ticket_ComplaintId` ON `Ticket` (`ComplaintId`);


ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `Filters` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedDate` datetime(6) NOT NULL,
    `Query` longtext CHARACTER SET utf8mb4 NULL,
    `Name` longtext CHARACTER SET utf8mb4 NULL,
    `Tags` longtext CHARACTER SET utf8mb4 NULL,
    `TenantId` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Filters` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `backart`.`jobstatus` (
  `Id` INT NOT NULL,
  `Message` VARCHAR(250) NOT NULL,
  `CreatedDate` DATETIME NOT NULL,
  `UpdatedDate` DATETIME NOT NULL,
  `TenantId` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`));

ALTER TABLE `backart`.`jobstatus` 
CHANGE COLUMN `Id` `Id` INT UNSIGNED NOT NULL AUTO_INCREMENT ,
ADD UNIQUE INDEX `Id_UNIQUE` (`Id` ASC) VISIBLE;

ALTER TABLE `backart`.`jobstatus` 
ADD COLUMN `CorelationId` VARCHAR(45) NULL AFTER `TenantId`;

CREATE TABLE `backart`.`mailsourceconfigs` (
  `Id` INT NOT NULL,
  `From` VARCHAR(455) NOT NULL,
  `Folders` VARCHAR(455) NOT NULL,
  `User` VARCHAR(45) NOT NULL,
  `Password` VARCHAR(45) NOT NULL,
  `DaysBefore` INT NOT NULL,
  PRIMARY KEY (`Id`));
  
ALTER TABLE `backart`.`mailsourceconfigs` 
CHANGE COLUMN `Id` `Id` INT NOT NULL AUTO_INCREMENT ;