function convertHHmmToSeconds(duration) {
  const [hoursStr, minutesStr] = duration.split('h');
  const [hours, minutes] = [parseInt(hoursStr), parseInt(minutesStr)];
  const totalSeconds = (hours * 3600) + (minutes * 60);

  if (!isNaN(totalSeconds) && duration.split('').pop() === 'm') 
    return totalSeconds
   
  return duration
}