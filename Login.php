<?php
    header('Content-Type: application/json');

    // Database configuration
    $DB_SERVER = "127.0.0.1";
    $DATABASE = "ar_aggression";   // Replace with your DB name
    $USERNAME_DB = "AR_Aggression_Assistance";       // Replace with your DB username
    $PASSWORD_DB = "FuturesLab123";           // Replace with your DB password

    // Connect to MySQL using mysqli
    $conn = new mysqli($DB_SERVER, $USERNAME_DB, $PASSWORD_DB, $DATABASE);

    // Check connection
    if ($conn->connect_error) {
        echo json_encode([
            "status" => "error",
            "message" => "Connection failed: " . $conn->connect_error
        ]);
        exit();
    }

    // Example: Fetch all users
    $sql = "SELECT ID, Username, Email, `Password` FROM account";
    $result = $conn->query($sql);

    if ($result) {
        $users = [];
        while ($row = $result->fetch_assoc()) {
            $users[] = $row;
        }
        echo json_encode([
            "status" => "success",
            "data" => $users
        ]);
    } else {
        echo json_encode([
            "status" => "error",
            "message" => "Query failed: " . $conn->error
        ]);
    }

    // Close connection
    $conn->close();
?>