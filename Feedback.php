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

    // Get POST data
    $UserID = isset($_POST['User_ID']) ? $_POST['User_ID'] : '';
    $Level = isset($_POST['Level']) ? $_POST['Level'] : '';
    $Feedback = isset($_POST['Feedback']) ? $_POST['Feedback'] : '';

    // Insert Feedback
    $insert_sql = $conn->prepare("INSERT INTO feedback (User_ID, `Level`, Feedback) VALUES (?, ?, ?)");
    $insert_sql->bind_param("sss", $UserID, $Level, $Feedback);

    if ($insert_sql->execute()) {
        echo json_encode([
            "status" => "success",
            "message" => "Feedback created successfully."
        ]);
    } else {
        echo json_encode([
            "status" => "error",
            "message" => "Failed to create feedback: " . $conn->error
        ]);
    }

    // Close connection
    $insert_sql->close();
    $conn->close();
?>