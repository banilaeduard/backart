--CREATE DATABASE backart /* SQLINES DEMO *** RACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /* SQLINES DEMO *** RYPTION='N' */;

-- SQLINES FOR EVALUATION USE ONLY (14 DAYS)
CREATE TABLE AspNetRoles (
    Id varchar(255) NOT NULL,
    Name varchar(256) NULL,
    NormalizedName varchar(256) NULL,
    ConcurrencyStamp varchar(max) NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
);


CREATE TABLE DataKeyLocation (
    Id varchar(255) NOT NULL,
    name varchar(255) NOT NULL,
    locationCode varchar(max) NULL,
    CONSTRAINT PK_DataKeyLocation PRIMARY KEY (Id),
    CONSTRAINT AK_DataKeyLocation_name UNIQUE (name)
);


CREATE TABLE AspNetRoleClaims (
    Id int NOT NULL IDENTITY,
    RoleId varchar(255) NOT NULL,
    ClaimType varchar(max) NULL,
    ClaimValue varchar(max) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);


CREATE TABLE AspNetUsers (
    Id varchar(255) NOT NULL,
    Name varchar(max) NULL,
    Address varchar(max) NULL,
    DataKeyLocationId varchar(255) NULL,
    Tenant varchar(max) NULL,
    UserName varchar(256) NULL,
    NormalizedUserName varchar(256) NULL,
    Email varchar(256) NULL,
    NormalizedEmail varchar(256) NULL,
    EmailConfirmed smallint NOT NULL,
    PasswordHash varchar(max) NULL,
    SecurityStamp varchar(max) NULL,
    ConcurrencyStamp varchar(max) NULL,
    PhoneNumber varchar(max) NULL,
    PhoneNumberConfirmed smallint NOT NULL,
    TwoFactorEnabled smallint NOT NULL,
    LockoutEnd datetime2(6) NULL,
    LockoutEnabled smallint NOT NULL,
    AccessFailedCount int NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUsers_DataKeyLocation_DataKeyLocationId FOREIGN KEY (DataKeyLocationId) REFERENCES DataKeyLocation (name)
);


CREATE TABLE AspNetUserClaims (
    Id int NOT NULL IDENTITY,
    UserId varchar(255) NOT NULL,
    ClaimType varchar(max) NULL,
    ClaimValue varchar(max) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);


CREATE TABLE AspNetUserLogins (
    LoginProvider varchar(255) NOT NULL,
    ProviderKey varchar(255) NOT NULL,
    ProviderDisplayName varchar(max) NULL,
    UserId varchar(255) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);


CREATE TABLE AspNetUserRoles (
    UserId varchar(255) NOT NULL,
    RoleId varchar(255) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);


CREATE TABLE AspNetUserTokens (
    UserId varchar(255) NOT NULL,
    LoginProvider varchar(255) NOT NULL,
    Name varchar(255) NOT NULL,
    Value varchar(max) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);


CREATE TABLE RefreshToken (
    Id int NOT NULL IDENTITY,
    Token varchar(max) NULL,
    Expires datetime2(6) NOT NULL,
    Created datetime2(6) NOT NULL,
    CreatedByIp varchar(max) NULL,
    Revoked datetime2(6) NULL,
    RevokedByIp varchar(max) NULL,
    ReplacedByToken varchar(max) NULL,
    AppIdentityUserId varchar(255) NOT NULL,
    CONSTRAINT PK_RefreshToken PRIMARY KEY (Id),
    CONSTRAINT FK_RefreshToken_AspNetUsers_AppIdentityUserId FOREIGN KEY (AppIdentityUserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
);


CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);


CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles (NormalizedName);


CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);


CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);


CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);


CREATE INDEX EmailIndex ON AspNetUsers (NormalizedEmail);


CREATE INDEX IX_AspNetUsers_DataKeyLocationId ON AspNetUsers (DataKeyLocationId);


CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers (NormalizedUserName);


CREATE INDEX IX_RefreshToken_AppIdentityUserId ON RefreshToken (AppIdentityUserId);



CREATE TABLE CodeAttribute (
    InnerValue varchar(255) NOT NULL,
    Tag varchar(255) NOT NULL,
    Id varchar(max) NULL,
    DisplayValue varchar(max) NULL,
    TenantId varchar(max) NULL,
    CONSTRAINT Id PRIMARY KEY (Tag, InnerValue)
);


CREATE TABLE Codes (
    Id int NOT NULL IDENTITY,
    CodeDisplay varchar(max) NULL,
    CodeValue varchar(max) NULL,
    AttributeTags varchar(max) NULL,
    CodeValueFormat varchar(max) NULL,
    isRoot smallint NOT NULL,
    args varchar(max) NULL,
    TenantId varchar(max) NULL,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    CodeLinkId int NULL,
    CONSTRAINT PK_Codes PRIMARY KEY (Id),
    CONSTRAINT FK_Codes_Codes_CodeLinkId FOREIGN KEY (CodeLinkId) REFERENCES Codes (Id)
);


CREATE INDEX IX_Codes_CodeLinkId ON Codes (CodeLinkId);


CREATE TABLE CodeAttributeSnapshot (
    InnerValue varchar(255) NOT NULL,
    Tag varchar(255) NOT NULL,
    Id varchar(max) NULL,
    DisplayValue varchar(max) NULL,
    TenantId varchar(max) NULL,
    CONSTRAINT Id PRIMARY KEY (Tag, InnerValue)
);


CREATE TABLE Complaints (
    Id int NOT NULL IDENTITY,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    TenantId varchar(max) NULL,
    Status varchar(max) NULL,
    NrComanda varchar(max) NULL,
    isDeleted smallint NOT NULL,
    DataKeyId varchar(255) NULL,
    CONSTRAINT PK_Complaints PRIMARY KEY (Id),
    CONSTRAINT FK_Complaints_DataKeyLocation_DataKeyId FOREIGN KEY (DataKeyId) REFERENCES DataKeyLocation (Id)
);


CREATE TABLE Ticket (
    Id int NOT NULL IDENTITY,
    CodeValue varchar(max) NULL,
    Description varchar(max) NULL,
    HasAttachments smallint NOT NULL,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    ComplaintId int NOT NULL,
    isDeleted smallint NOT NULL,
    CONSTRAINT PK_Ticket PRIMARY KEY (Id),
    CONSTRAINT FK_Ticket_Complaints_ComplaintId FOREIGN KEY (ComplaintId) REFERENCES Complaints (Id)
);


CREATE TABLE Attachment (
    Id int NOT NULL IDENTITY,
    Data varchar(max) NULL,
    Title varchar(max) NULL,
    TicketId int NOT NULL,
    Extension varchar(max) NULL,
    ContentType varchar(max) NULL,
    StorageType varchar(max) NULL,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    CONSTRAINT PK_Attachment PRIMARY KEY (Id),
    CONSTRAINT FK_Attachment_Ticket_TicketId FOREIGN KEY (TicketId) REFERENCES Ticket (Id)
);


CREATE TABLE CodeLinkSnapshot (
    Id int NOT NULL IDENTITY,
    CodeDisplay varchar(max) NULL,
    CodeValue varchar(max) NULL,
    AttributeTags varchar(max) NULL,
    CodeValueFormat varchar(max) NULL,
    isRoot smallint NOT NULL,
    args varchar(max) NULL,
    TenantId varchar(max) NULL,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    CodeLinkId int NULL,
    TicketId int NULL,
    CONSTRAINT PK_CodeLinkSnapshot PRIMARY KEY (Id),
    CONSTRAINT FK_CodeLinkSnapshot_CodeLinkSnapshot_CodeLinkId FOREIGN KEY (CodeLinkId) REFERENCES CodeLinkSnapshot (Id),
    CONSTRAINT FK_CodeLinkSnapshot_Ticket_TicketId FOREIGN KEY (TicketId) REFERENCES Ticket (Id)
);


CREATE INDEX IX_Attachment_TicketId ON Attachment (TicketId);


CREATE INDEX IX_CodeLinkSnapshot_CodeLinkId ON CodeLinkSnapshot (CodeLinkId);


CREATE INDEX IX_CodeLinkSnapshot_TicketId ON CodeLinkSnapshot (TicketId);


CREATE INDEX IX_Complaints_DataKeyId ON Complaints (DataKeyId);


CREATE INDEX IX_Ticket_ComplaintId ON Ticket (ComplaintId);




CREATE TABLE Filters (
    Id int NOT NULL IDENTITY,
    CreatedDate datetime2(6) NOT NULL,
    UpdatedDate datetime2(6) NOT NULL,
    Query varchar(max) NULL,
    Name varchar(max) NULL,
    Tags varchar(max) NULL,
    TenantId varchar(max) NULL,
    CONSTRAINT PK_Filters PRIMARY KEY (Id)
);


CREATE TABLE jobstatus (
  Id INT NOT NULL IDENTITY,
  Message VARCHAR(250) NOT NULL,
  CreatedDate DATETIME2(0) NOT NULL,
  UpdatedDate DATETIME2(0) NOT NULL,
  TenantId VARCHAR(45) NOT NULL,
  CorelationId VARCHAR(45) NULL
  PRIMARY KEY (Id));

CREATE TABLE mailsourceconfigs (
  Id INT NOT NULL IDENTITY,
  [From] VARCHAR(455) NOT NULL,
  Folders VARCHAR(455) NOT NULL,
  [User] VARCHAR(45) NOT NULL,
  Password VARCHAR(45) NOT NULL,
  DaysBefore INT NOT NULL,
  PRIMARY KEY (Id));