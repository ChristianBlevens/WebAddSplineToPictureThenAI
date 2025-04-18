mergeInto(LibraryManager.library, {
  InitializeWebCamera: function (objectName, functionName, width, height) {
    // Store Unity callback info without creating DOM elements
    window.unityWebCamera = {
      objectName: UTF8ToString(objectName),
      functionName: UTF8ToString(functionName),
      width: width,
      height: height,
      stream: null,
      active: false
    };
    
    // Create hidden video element to access the stream
    var video = document.createElement('video');
    video.id = 'unity-webcam-video';
    video.autoplay = true;
    video.style.position = 'absolute';
    video.style.top = '0';
    video.style.left = '0';
    video.style.width = '1px';
    video.style.height = '1px';
    video.style.opacity = '0.01';
    document.body.appendChild(video);
    
    // Create hidden canvas for capturing frames
    // Using the dimensions passed from Unity (from the RectTransform)
    var canvas = document.createElement('canvas');
    canvas.id = 'unity-webcam-canvas';
    canvas.width = width;
    canvas.height = height;
    canvas.style.display = 'none';
    document.body.appendChild(canvas);
    
    window.unityWebCamera.video = video;
    window.unityWebCamera.canvas = canvas;
    
    // Set up interval to send frames to Unity when camera is active
    window.unityWebCamera.frameInterval = null;
  },
  
  // New function to update canvas size after initialization
  UpdateCanvasSize: function (width, height) {
    if (!window.unityWebCamera || !window.unityWebCamera.canvas) {
      console.error("Cannot update canvas size: Camera not initialized");
      return;
    }
    
    // Update stored dimensions
    window.unityWebCamera.width = width;
    window.unityWebCamera.height = height;
    
    // Update canvas dimensions
    window.unityWebCamera.canvas.width = width;
    window.unityWebCamera.canvas.height = height;
    
    console.log("Canvas size updated to: " + width + "x" + height);
    
    // If the camera is active, send a notification that the size has changed
    if (window.unityWebCamera.active) {
      SendMessage(
        window.unityWebCamera.objectName,
        window.unityWebCamera.functionName,
        "CANVAS_RESIZED:" + width + "x" + height
      );
    }
  },
  
  StartWebCamera: function () {
    if (!window.unityWebCamera) return;
    
    // Request camera access
    navigator.mediaDevices.getUserMedia({ video: true })
      .then(function(stream) {
        window.unityWebCamera.stream = stream;
        window.unityWebCamera.video.srcObject = stream;
        window.unityWebCamera.active = true;
        
        // Notify Unity that camera is ready
        SendMessage(
          window.unityWebCamera.objectName,
          window.unityWebCamera.functionName,
          "CAMERA_READY"
        );
        
        // Start sending frames to Unity at intervals
        if (window.unityWebCamera.frameInterval) {
          clearInterval(window.unityWebCamera.frameInterval);
        }
        
        window.unityWebCamera.frameInterval = setInterval(function() {
          if (window.unityWebCamera.active) {
            try {
              // Draw current video frame to canvas
              var ctx = window.unityWebCamera.canvas.getContext('2d');
              ctx.drawImage(
                window.unityWebCamera.video, 
                0, 0, 
                window.unityWebCamera.width, 
                window.unityWebCamera.height
              );
              
              // Get frame as base64 data URL (without the prefix)
              var dataUrl = window.unityWebCamera.canvas.toDataURL('image/jpeg', 0.7);
              var base64Data = dataUrl.split(',')[1];
              
              // Send frame to Unity
              SendMessage(
                window.unityWebCamera.objectName, 
                "OnWebCameraFrame", 
                base64Data
              );
            } catch (err) {
              console.error("Error sending camera frame to Unity:", err);
            }
          }
        }, 100); // Send 10 frames per second to balance performance
      })
      .catch(function(err) {
        console.error("Camera access error: ", err);
        SendMessage(
          window.unityWebCamera.objectName,
          window.unityWebCamera.functionName,
          "CAMERA_ERROR:" + err.message
        );
      });
  },
  
  StopWebCamera: function () {
    if (!window.unityWebCamera) return;
    
    // Clear frame interval
    if (window.unityWebCamera.frameInterval) {
      clearInterval(window.unityWebCamera.frameInterval);
      window.unityWebCamera.frameInterval = null;
    }
    
    // Stop camera stream if active
    if (window.unityWebCamera.stream) {
      window.unityWebCamera.stream.getTracks().forEach(function(track) {
        track.stop();
      });
      window.unityWebCamera.stream = null;
    }
    
    window.unityWebCamera.active = false;
  },
  
  ShowFilePickerDialog: function() {
    if (!window.unityWebCamera) return;
    
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*';
    
    input.onchange = function(e) {
      var file = e.target.files[0];
      if (!file) return;
      
      var reader = new FileReader();
      reader.onload = function(event) {
        var dataUrl = event.target.result;
        var base64Data = dataUrl.split(',')[1];
        
        SendMessage(
          window.unityWebCamera.objectName,
          window.unityWebCamera.functionName,
          "IMAGE_LOADED:" + base64Data
        );
      };
      reader.readAsDataURL(file);
    };
    
    input.click();
  }
});