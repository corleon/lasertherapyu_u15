import { LitElement, html, css } from 'https://cdn.skypack.dev/lit@3';

export default class WebinarDashboardElement extends LitElement {
    static styles = css`
        :host {
            display: block;
            font-family: Arial, sans-serif;
            background-color: #f5f5f5;
            padding: 20px;
        }
        
        .container {
            max-width: 1000px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        
        h1 {
            color: #333;
            border-bottom: 3px solid #1b264f;
            padding-bottom: 10px;
        }
        
        .section {
            margin: 30px 0;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 6px;
            background-color: #fafafa;
        }
        
        .section h2 {
            margin-top: 0;
            color: #1b264f;
        }
        
        textarea {
            width: 100%;
            height: 200px;
            padding: 10px;
            border: 1px solid #ccc;
            border-radius: 4px;
            font-family: monospace;
            font-size: 12px;
            resize: vertical;
        }
        
        button {
            background-color: #1b264f;
            color: white;
            padding: 12px 24px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            margin-right: 10px;
            margin-top: 10px;
        }
        
        button:hover {
            background-color: #2c3e50;
        }
        
        button:disabled {
            background-color: #95a5a6;
            cursor: not-allowed;
        }
        
        .success {
            color: #27ae60;
            background-color: #d5f4e6;
            padding: 10px;
            border-radius: 4px;
            margin: 10px 0;
        }
        
        .error {
            color: #e74c3c;
            background-color: #fadbd8;
            padding: 10px;
            border-radius: 4px;
            margin: 10px 0;
        }
        
        .info {
            color: #3498db;
            background-color: #ebf3fd;
            padding: 10px;
            border-radius: 4px;
            margin: 10px 0;
        }
        
        .webinar-item {
            background: white;
            margin: 10px 0;
            padding: 15px;
            border-radius: 4px;
            border-left: 4px solid #1b264f;
        }
        
        .webinar-title {
            font-weight: bold;
            color: #1b264f;
            margin-bottom: 5px;
        }
        
        .webinar-details {
            font-size: 12px;
            color: #666;
        }
        
        input[type="number"] {
            width: 100px;
            padding: 8px;
            border: 1px solid #ccc;
            border-radius: 4px;
        }
        
        label {
            display: inline-block;
            width: 150px;
            font-weight: bold;
        }
        
        .step {
            margin: 20px 0;
            padding: 15px;
            background: #f8f9fa;
            border-left: 4px solid #1b264f;
        }
        
        .step h3 {
            margin-top: 0;
            color: #1b264f;
        }
        
        small {
            color: #666;
            margin-left: 10px;
        }
    `;

    static properties = {
        parsedWebinars: { type: Array, state: true },
        messages: { type: Array, state: true },
        previewHtml: { type: String, state: true },
        isCreatingDocType: { type: Boolean, state: true },
        isPreviewing: { type: Boolean, state: true },
        isImporting: { type: Boolean, state: true }
    };

    constructor() {
        super();
        this.parsedWebinars = [];
        this.messages = [];
        this.previewHtml = '';
        this.isCreatingDocType = false;
        this.isPreviewing = false;
        this.isImporting = false;
    }

    render() {
        return html`
            <div class="container">
                <h1>Veterinary Webinar Import Dashboard</h1>
                
                <div class="info">
                    <strong>Instructions:</strong>
                    <ol>
                        <li>First, create the document type by clicking "Create Document Type"</li>
                        <li>Paste your CSV content in the textarea below</li>
                        <li>Click "Preview Data" to see what will be imported</li>
                        <li>Set the parent node ID (use -1 for root)</li>
                        <li>Click "Import Webinars" to complete the import</li>
                    </ol>
                </div>

                <div class="step">
                    <h3>Step 1: Create Document Type</h3>
                    <button 
                        @click=${this.createDocumentType}
                        ?disabled=${this.isCreatingDocType}>
                        ${this.isCreatingDocType ? 'Creating...' : 'Create Document Type'}
                    </button>
                </div>

                <div class="step">
                    <h3>Step 2: Paste CSV Content</h3>
                    <textarea 
                        id="csvContent" 
                        placeholder="Paste your CSV content here...">itemname,fieldname,value,
Vet October-2018,Facebook Description,Learn how laser therapy can help treat common conditions in bovine medicine in this free webinar from Laser Therapy U.,
Vet October-2018,Date,20181029T000000,
Vet October-2018,Video Embed Code,"<iframe id=""vzvd-17825205"" name=""vzvd-17825205"" title=""video player"" type=""text/html"" width=""640"" height=""360"" frameborder=""0"" allowfullscreen allowTransparency=""true"" src=""https://view.vzaar.com/17825205/player"" allow=""autoplay"" class=""video-player""></iframe>",
Vet October-2018,Title,Veterinary - Shedding New Light on Bovine Medicine,
Vet October-2018,Length,49:32:00,</textarea>
                    <br>
                    <button 
                        @click=${this.previewData}
                        ?disabled=${this.isPreviewing}>
                        ${this.isPreviewing ? 'Parsing...' : 'Preview Data'}
                    </button>
                </div>

                <div class="step">
                    <h3>Step 3: Configure Import</h3>
                    <label for="parentNodeId">Parent Node ID:</label>
                    <input type="number" id="parentNodeId" value="-1" placeholder="-1">
                    <small>(Use -1 for root level, or specify a parent node ID)</small>
                </div>

                <div class="step">
                    <h3>Step 4: Import</h3>
                    <button 
                        @click=${this.importWebinars}
                        ?disabled=${this.isImporting || this.parsedWebinars.length === 0}>
                        ${this.isImporting ? 'Importing...' : 'Import Webinars'}
                    </button>
                </div>

                <div id="messages">
                    ${this.messages.map(msg => html`
                        <div class="${msg.type}">${msg.text}</div>
                    `)}
                </div>

                <div id="preview">
                    ${this.previewHtml ? html`<div .innerHTML=${this.previewHtml}></div>` : ''}
                </div>
            </div>
        `;
    }

    async createDocumentType() {
        this.isCreatingDocType = true;

        try {
            const response = await fetch('/umbraco/backoffice/api/WebinarImport/CreateDocumentType', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showMessage('Document type created successfully!', 'success');
            } else {
                this.showMessage('Failed to create document type: ' + result.message, 'error');
            }
        } catch (error) {
            this.showMessage('Error creating document type: ' + error.message, 'error');
        } finally {
            this.isCreatingDocType = false;
        }
    }

    async previewData() {
        const csvContent = this.shadowRoot.querySelector('#csvContent').value.trim();
        
        if (!csvContent) {
            this.showMessage('Please paste CSV content first', 'error');
            return;
        }

        this.isPreviewing = true;

        try {
            // Parse data locally for preview
            this.parsedWebinars = this.parseCsvContent(csvContent);
            
            let previewHtml = `<h2>Preview (${this.parsedWebinars.length} webinars found)</h2>`;
            
            this.parsedWebinars.forEach(webinar => {
                previewHtml += `
                    <div class="webinar-item">
                        <div class="webinar-title">${webinar.Title || webinar.ItemName}</div>
                        <div class="webinar-details">
                            Date: ${webinar.Date || 'Not specified'} | 
                            Length: ${webinar.Length || 'Not specified'} |
                            Display Name: ${webinar.DisplayName || 'Not specified'}
                        </div>
                    </div>
                `;
            });
            
            this.previewHtml = previewHtml;
            
            this.showMessage(`Successfully parsed ${this.parsedWebinars.length} webinars`, 'success');
            
        } catch (error) {
            this.showMessage('Error parsing CSV: ' + error.message, 'error');
        } finally {
            this.isPreviewing = false;
        }
    }

    async importWebinars() {
        const csvContent = this.shadowRoot.querySelector('#csvContent').value.trim();
        const parentNodeId = parseInt(this.shadowRoot.querySelector('#parentNodeId').value) || -1;
        
        if (!csvContent) {
            this.showMessage('Please paste CSV content first', 'error');
            return;
        }

        this.isImporting = true;

        try {
            const response = await fetch('/umbraco/backoffice/api/WebinarImport/ImportWebinars', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    csvContent: csvContent,
                    parentNodeId: parentNodeId
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                this.showMessage(`Successfully imported ${result.count} webinars!`, 'success');
            } else {
                this.showMessage('Failed to import webinars: ' + result.message, 'error');
            }
        } catch (error) {
            this.showMessage('Error importing webinars: ' + error.message, 'error');
        } finally {
            this.isImporting = false;
        }
    }

    parseCsvContent(csvContent) {
        const webinars = {};
        const lines = csvContent.split('\n').filter(line => line.trim());
        
        // Skip header
        for (let i = 1; i < lines.length; i++) {
            const parts = this.parseCsvLine(lines[i]);
            if (parts.length >= 3) {
                const itemName = parts[0].trim();
                const fieldName = parts[1].trim();
                const value = parts[2].trim().replace(/^"|"$/g, '');
                
                if (!webinars[itemName]) {
                    webinars[itemName] = { ItemName: itemName };
                }
                
                // Map field names to properties
                const fieldMap = {
                    'title': 'Title',
                    'description': 'Description',
                    'date': 'Date',
                    'length': 'Length',
                    'video embed code': 'VideoEmbedCode',
                    'page title': 'PageTitle',
                    'meta description': 'MetaDescription',
                    'facebook title': 'FacebookTitle',
                    'facebook description': 'FacebookDescription',
                    'hide from flyout': 'HideFromFlyout',
                    'active': 'Active',
                    'product': 'Product',
                    'products': 'Products',
                    'tags': 'Tags',
                    '__display name': 'DisplayName'
                };
                
                const propertyName = fieldMap[fieldName.toLowerCase()];
                if (propertyName) {
                    webinars[itemName][propertyName] = value;
                }
            }
        }
        
        return Object.values(webinars);
    }

    parseCsvLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;
        
        for (let i = 0; i < line.length; i++) {
            const char = line[i];
            
            if (char === '"') {
                if (inQuotes && i + 1 < line.length && line[i + 1] === '"') {
                    current += '"';
                    i++; // skip next quote
                } else {
                    inQuotes = !inQuotes;
                }
            } else if (char === ',' && !inQuotes) {
                result.push(current);
                current = '';
            } else {
                current += char;
            }
        }
        
        result.push(current);
        return result;
    }

    showMessage(message, type) {
        this.messages = [...this.messages, { text: message, type: type }];
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            this.messages = this.messages.filter(msg => msg.text !== message || msg.type !== type);
        }, 5000);
    }
}

customElements.define('webinar-dashboard-element', WebinarDashboardElement);