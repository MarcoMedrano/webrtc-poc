const stun = require('stun');

// const stunServer = 'stun.l.google.com:19302';
const stunServer = 'localhost:3478';

stun.request(stunServer, (err, res) => {
    if (err) {
        console.error(err);
    } else {
        const address = res.getXorAddress();
        console.log('XOR Address', address);
    }
});
