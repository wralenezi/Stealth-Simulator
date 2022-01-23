mergeInto(LibraryManager.library, {

  Speak: function (pitch,volume,voiceIndex,msg)
  {
    msg = Pointer_stringify(msg);

    // Prepare the message to be spoken
    var msgSynth = new SpeechSynthesisUtterance(msg);
    
    // Language list
    const langList = ["en-US"]; 
    // Set the language
    msgSynth.lang = langList[0];

    // Set the volume it range: 0 to 1
    msgSynth.volume = volume; 

    // The speed at which the utterance will be spoken at; 0.1 to 10
    msgSynth.rate = 1;

    // the pitch at which the utterance will be spoken at; 0 to 2
    msgSynth.pitch = pitch;

    // Get the object of speech synthesis
    var winSynth = window.speechSynthesis;

    // Set get the available voices
    var voices = winSynth.getVoices();
    msgSynth.voice = voices[voiceIndex];

    // stop any TTS that may still be active
    winSynth.cancel();

    // play the message
    winSynth.speak(msgSynth);
  },

});