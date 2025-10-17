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
    $username = isset($_POST['username']) ? $_POST['username'] : '';
    $email = isset($_POST['email']) ? $_POST['email'] : '';
    $phonenumber = isset($_POST['phonenumber']) ? $_POST['phonenumber'] : '';
    $password = isset($_POST['password']) ? $_POST['password'] : '';

    // Basic validation
    if (empty($username) || empty($password)) {
        echo json_encode([
            "status" => "error",
            "message" => "Username and password are required."
        ]);
        exit();
    }

    // Check if username already exists
    $check_sql = $conn->prepare("SELECT ID FROM account WHERE Username = ?");
    $check_sql->bind_param("s", $username);
    $check_sql->execute();
    $check_sql->store_result();

    if ($check_sql->num_rows > 0) {
        echo json_encode([
            "status" => "error",
            "message" => "Username already exists."
        ]);
        $check_sql->close();
        $conn->close();
        exit();
    }
    $check_sql->close();

    // Insert new user
    $insert_sql = $conn->prepare("INSERT INTO account (Username, Email, PhoneNumber, `Password`) VALUES (?, ?, ?, ?)");
    $insert_sql->bind_param("ssss", $username, $email, $phonenumber, $password);

    if ($insert_sql->execute()) {
        echo json_encode([
            "status" => "success",
            "message" => "Account created successfully."
        ]);
    } else {
        echo json_encode([
            "status" => "error",
            "message" => "Failed to create account: " . $conn->error
        ]);
    }

    // Close connection
    $insert_sql->close();
    $conn->close();
?>