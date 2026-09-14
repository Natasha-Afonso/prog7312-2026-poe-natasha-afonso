 USE master;
GO
-- Create the SmartX database only if it does not already exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartX')
BEGIN
    CREATE DATABASE SmartX;
END
GO
-- Switch to the SmartX database
USE SmartX;
GO
-- Create DeploymentNodes Table 
CREATE TABLE DeploymentNodes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    ParentId INT NULL,
    CONSTRAINT FK_DeploymentNodes_Parent FOREIGN KEY (ParentId)
        REFERENCES DeploymentNodes(Id)
);

-- Create Sensors Table (Includes FilePath column)
CREATE TABLE Sensors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DeviceIdentifier NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    DeploymentNodeId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    LastReading NVARCHAR(50) NULL,
    FilePath NVARCHAR(255) NULL,
    RegisteredAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Sensors_DeploymentNode FOREIGN KEY (DeploymentNodeId)
        REFERENCES DeploymentNodes(Id)
);

-- Create SensorReadings Table
CREATE TABLE SensorReadings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SensorId INT NOT NULL,
    MetricName NVARCHAR(100) NOT NULL,
    Value FLOAT NOT NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_SensorReadings_Sensor FOREIGN KEY (SensorId)
        REFERENCES Sensors(Id)
);

CREATE TABLE Alerts (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SensorId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Metric NVARCHAR(100) NOT NULL,
    Value NVARCHAR(50) NOT NULL,
    Threshold NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'unread',
    Timestamp DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Alerts_Sensor FOREIGN KEY (SensorId)
        REFERENCES Sensors(Id)
);
GO

-- Insert Dummy DeploymentNodes (Facility -> Zone -> Sub-Zone)
INSERT INTO DeploymentNodes (Name, ParentId)
VALUES
('Facility A', NULL),
('Zone 1', 1),
('Sub-Zone B', 2),
('Zone 2', 1);

-- Insert Dummy Sensors (Includes FilePath values)
INSERT INTO Sensors (DeviceIdentifier, Category, DeploymentNodeId, Status, LastReading, FilePath)
VALUES
('AA:BB:CC:00:01', 'Environmental', 3, 'active', '27.4C', 'uploads/temp_spec_001.pdf'),
('AA:BB:CC:00:02', 'Power Consumption', 4, 'active', '61.2W', NULL),
('AA:BB:CC:00:03', 'Environmental', 3, 'warning', '38.7C', 'uploads/temp_spec_003.pdf');

-- Insert Dummy SensorReadings
INSERT INTO SensorReadings (SensorId, MetricName, Value)
VALUES
(1, 'Temperature', 27.4),
(2, 'Wattage', 61.2),
(3, 'Temperature', 38.7);

INSERT INTO Alerts (SensorId, Title, Message, Metric, Value, Threshold, Status)
VALUES
(3, 'TEMPERATURE THRESHOLD EXCEEDED', 'Sensor has reported a temperature outside the configured safe operating range.', 'Temperature', '38.7C', '35.0C', 'unread');
GO