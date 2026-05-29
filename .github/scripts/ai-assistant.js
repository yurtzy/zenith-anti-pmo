const fs = require('fs');
const path = require('path');

async function run({ github, context }) {
  const geminiKey = process.env.GEMINI_API_KEY;
  if (!geminiKey) {
    console.error("GEMINI_API_KEY secret is not set.");
    return;
  }

  // 1. Read copilot-instructions.md
  let instructions = "";
  try {
    instructions = fs.readFileSync(path.join(process.cwd(), '.github', 'copilot-instructions.md'), 'utf8');
  } catch (err) {
    console.warn("Could not read copilot-instructions.md:", err.message);
  }

  // 2. Identify if it is an Issue or PR
  const isIssue = !!context.payload.issue;
  const isPR = !!context.payload.pull_request;
  
  let title = "";
  let body = "";
  let number = 0;
  
  if (isIssue) {
    title = context.payload.issue.title;
    body = context.payload.issue.body || "";
    number = context.payload.issue.number;
  } else if (isPR) {
    title = context.payload.pull_request.title;
    body = context.payload.pull_request.body || "";
    number = context.payload.pull_request.number;
  } else {
    console.log("Unsupported event trigger.");
    return;
  }

  // 3. Construct the prompt
  const systemPrompt = `You are the official Zenith Suite AI Assistant. Your task is to welcome contributors, analyze new issues/pull requests, provide technical feedback, outline potential next steps, and ensure changes align with our strict repository guidelines.

Strict Guidelines:
${instructions}

Remember: Never expose sensitive email addresses (such as lionelgaming256@gmail.com) in any responses. Keep all answers clean, professional, and aligned with the matte-black borderless C# / vanilla extension architecture.`;

  const userPrompt = `A new ${isIssue ? 'Issue' : 'Pull Request'} has been opened:
Title: ${title}
Body:
${body}

Please write a helpful, constructive comment responding to this ${isIssue ? 'issue' : 'PR'}. Format your response nicely in Markdown. Include a welcoming opening, a technical summary of what needs to be done or reviewed, and list specific next steps. Keep it professional.`;

  // 4. Call Gemini API
  const url = `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=${geminiKey}`;
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      contents: [{
        role: "user",
        parts: [{ text: userPrompt }]
      }],
      systemInstruction: {
        parts: [{ text: systemPrompt }]
      },
      generationConfig: {
        temperature: 0.7,
        maxOutputTokens: 1000
      }
    })
  });

  if (!response.ok) {
    const errText = await response.text();
    throw new Error(`Gemini API error: ${response.status} - ${errText}`);
  }

  const result = await response.json();
  const aiResponse = result.candidates?.[0]?.content?.parts?.[0]?.text;
  
  if (!aiResponse) {
    throw new Error("No response generated from Gemini API.");
  }

  // 5. Post comment on the issue/PR
  await github.rest.issues.createComment({
    owner: context.repo.owner,
    repo: context.repo.repo,
    issue_number: number,
    body: aiResponse
  });

  console.log(`Successfully replied to ${isIssue ? 'Issue' : 'PR'} #${number}`);
}

module.exports = run;
