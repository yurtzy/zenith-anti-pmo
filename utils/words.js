// PureMind / Zenith Anti-PMO - Content Filter Dictionary
// Highly curated list of explicit domains, keywords, and search queries for trigger detection.
// Keeps the code 100% professional and clean.

const ZenithDictionary = {
  // Common adult website domains (partial matching for efficiency and security)
  blockedDomains: [
    'pornhub.com',
    'xvideos.com',
    'xnxx.com',
    'xhamster.com',
    'redtube.com',
    'youporn.com',
    'spankbang.com',
    'tube8.com',
    'chaturbate.com',
    'onlyfans.com',
    'cam4.com',
    'bongacams.com',
    'livejasmin.com',
    'stripchat.com',
    'hentaihaven.red',
    'nhentai.net',
    'gelbooru.com',
    'rule34.xxx',
    'tnaflix.com',
    'eporner.com',
    'hqpornportal.com',
    'yourporn.sexy',
    'adultempire.com',
    'brazzers.com',
    'naughtyamerica.com',
    'realitykings.com',
    'mofog.com',
    'heavy-r.com',
    'xhamster.desi',
    'porntrex.com',
    'pornhubpremium.com',
    'hotgirls.to',
    'nekopoi.care',
    'nekopoi.org',
    'nekopoi.net',
    'civitai.com',
    'janitorai.com',
    'novelai.net',
    'crushon.ai',
    'pixai.art'
  ],

  // Specific keywords to detect in URLs or search queries
  triggerKeywords: [
    'porn',
    'pornography',
    'xxx',
    'hentai',
    'ecchi',
    'erotic',
    'nsfw',
    'milf',
    'xvideos',
    'xnxx',
    'xhamster',
    'pornhub',
    'masturbation',
    'masturbate',
    'orgasm',
    'blowjob',
    'cuckold',
    'gangbang',
    'camgirl',
    'eroge',
    'rule34',
    'doujinshi',
    'nhentai',
    'playboy',
    'nekopoi',
    'hentaihaven',
    'javfree',
    'jav censorship',
    'bukkake',
    'nsfw prompt',
    'nsfw generator',
    'stable diffusion nsfw',
    'comfyui nsfw',
    'civitai',
    'janitorai',
    'sillytavern',
    'crushon.ai',
    'character.ai nsfw',
    'jailbreak prompt',
    'bokep',
    'sange',
    'ngocok',
    'lendir',
    'cersex',
    'colay',
    'colok',
    'seks',
    'mesum',
    'porno',
    'semi'
  ],

  // Common adult paths/categories that might appear inside normal websites
  blockedPaths: [
    '/porn/',
    '/nsfw/',
    '/hentai/',
    '/xxx/',
    '/erotica/'
  ]
};

// Expose dictionary for browser background service worker
if (typeof module !== 'undefined' && module.exports) {
  module.exports = ZenithDictionary;
} else {
  self.ZenithDictionary = ZenithDictionary;
}
