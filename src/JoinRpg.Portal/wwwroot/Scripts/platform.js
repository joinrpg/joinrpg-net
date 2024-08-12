
function Platform() {

  const uap = new UAParser();
  const device = uap.getDevice();
  const os = uap.getOS();

  const osName = os.name.toLowerCase();

  if (osName.indexOf('ios') > -1) {
    if (device.type === 'tablet' || device.type === 'mobile') {
      this.type = 'ios';
      return;
    }
  } else if (osName.indexOf('android') > -1) {
    if (device.type === 'tablet' || device.type === 'mobile') {
      this.type = 'android';
      return;
    }
  }

  this.type = 'desktop';
}

document.platform = new Platform();
