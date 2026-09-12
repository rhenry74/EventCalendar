import React, { useRef, useState, useEffect } from 'react';
import { 
  Dialog, 
  DialogTitle, 
  DialogContent, 
  DialogActions, 
  Button, 
  TextField,
  Box,
  FormControlLabel,
  Switch,
  Tabs,
  Tab,
  Stack,
  Tooltip,
  Typography,
  MenuItem
} from '@mui/material';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { format } from 'date-fns';
import type { Event, PartialEvent } from '../types';
import type { Theme } from '@mui/material/styles';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import categoriesData from '../../categories.json';

const categories = categoriesData as Array<{ name: string; icon: string; type: string; primaryColor: string; secondaryColor: string }>;

// Date-only event values are calendar dates, not UTC instants. Construct them
// in local time so opening and saving an event does not shift it across a day.
const parseEventDate = (value: string) => {
  const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (dateOnly) return new Date(Number(dateOnly[1]), Number(dateOnly[2]) - 1, Number(dateOnly[3]));
  return new Date(value);
};

interface EventDialogProps {
  open: boolean;
  onClose: () => void;
  initialEvent?: Event;
  onSave: (event: PartialEvent) => void;
  theme: Theme;
}

const EventDialog: React.FC<EventDialogProps> = ({ open, onClose, initialEvent, onSave, theme }) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [location, setLocation] = useState('');
  const [category, setCategory] = useState('General');
  const [date, setDate] = useState<Date | null>(null);
  const [hasTime, setHasTime] = useState(false);
  const [endDate, setEndDate] = useState<Date | null>(null);
  const [hasEndDate, setHasEndDate] = useState(false);
  const [hasEndTime, setHasEndTime] = useState(false);
  const [isPublic, setIsPublic] = useState(false);
  const [journal, setJournal] = useState('');
  const [journalTab, setJournalTab] = useState(0);
  const [journalOpen, setJournalOpen] = useState(false);
  const journalInputRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    if (initialEvent) {
      setTitle(initialEvent.title);
      setDescription(initialEvent.description || '');
      setLocation(initialEvent.location || '');
      setCategory(initialEvent.category || 'General');
      setDate(parseEventDate(initialEvent.date));
      setHasTime(/[T ]\d{2}:\d{2}:[0-9]/.test(initialEvent.date) && !/[T ]00:00:00(?:\.000)?(?:Z)?$/.test(initialEvent.date));
      setEndDate(initialEvent.endDate ? parseEventDate(initialEvent.endDate) : null);
      setHasEndDate(!!initialEvent.endDate);
      setHasEndTime(!!initialEvent.endDate && /[T ]\d{2}:\d{2}:[0-9]/.test(initialEvent.endDate));
      setIsPublic(!!initialEvent.isPublic);
      setJournal(initialEvent.journal || '');
      setJournalTab(0);
    } else {
      setTitle('');
      setDescription('');
      setLocation('');
      setCategory('General');
      setDate(new Date());
      setHasTime(false);
      setEndDate(null);
      setHasEndDate(false);
      setHasEndTime(false);
      setIsPublic(false);
      setJournal('');
      setJournalTab(0);
    }
  }, [initialEvent]);

  useEffect(() => {
    if (!open && !initialEvent) {
      setTitle('');
      setDescription('');
      setLocation('');
      setCategory('General');
      setDate(new Date());
      setHasTime(false);
      setEndDate(null);
      setHasEndDate(false);
      setHasEndTime(false);
      setIsPublic(false);
      setJournal('');
      setJournalTab(0);
    }
  }, [open]);

  const handleSave = () => {
    if (!title.trim()) {
      alert("Title is required");
      return;
    }

    const startDay = date ? format(date, 'yyyy-MM-dd') : '';
    const endDay = endDate ? format(endDate, 'yyyy-MM-dd') : '';
    if (hasEndDate && date && endDate && ((hasEndTime && endDate < date) || (!hasEndTime && endDay < startDay))) {
      alert('End date and time must be after the start date and time.');
      return;
    }
    
    const eventData: PartialEvent = {
      id: initialEvent?.id || '',
      title: title.trim(),
      description: description.trim(),
      location: location.trim(),
      category,
      date: date ? (hasTime ? date.toISOString() : format(date, 'yyyy-MM-dd')) : format(new Date(), 'yyyy-MM-dd'),
      isPublic,
      journal,
      endDate: hasEndDate && endDate ? (hasEndTime ? endDate.toISOString() : format(endDate, 'yyyy-MM-dd')) : undefined,
    };
    
    onSave(eventData);
    onClose();
  };

  const handleDateChange = (newValue: Date | null) => {
    setDate(newValue || new Date());
  };

  const closeEventEditor = () => {
    setJournalOpen(false);
    onClose();
  };

  const insertMarkdown = (prefix: string, suffix = '') => {
    const input = journalInputRef.current;
    if (!input) {
      setJournal(value => `${value}${value ? '\n' : ''}${prefix}text${suffix}`);
      return;
    }
    const start = input.selectionStart;
    const end = input.selectionEnd;
    const selected = journal.slice(start, end) || 'text';
    const replacement = `${prefix}${selected}${suffix}`;
    const nextValue = `${journal.slice(0, start)}${replacement}${journal.slice(end)}`;
    setJournal(nextValue);
    requestAnimationFrame(() => {
      input.focus();
      input.setSelectionRange(start + prefix.length, start + prefix.length + selected.length);
    });
  };

  if (!open) return null;

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Dialog 
        open={open} 
        onClose={closeEventEditor}
        fullWidth 
        maxWidth="lg"
        sx={{
          backgroundColor: theme.palette.background.default,
          borderRadius: 2,
          border: `1px solid ${theme.palette.divider}`,
          boxShadow: theme.shadows[3],
          '& .MuiDialog-paper': {
            backgroundColor: theme.palette.background.default,
            margin: { xs: 1, sm: 2 },
            width: '100%',
            boxShadow: theme.shadows[3]
          }
        }}
      >
        <DialogTitle 
          sx={{ 
            color: theme.palette.text.primary,
            borderBottom: `1px solid ${theme.palette.divider}`
          }}
        >
          {initialEvent ? 'Edit Event' : 'Add New Event'}
        </DialogTitle>
        <DialogContent sx={{ color: theme.palette.text.secondary }}>
          <Box 
            sx={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: 2, 
              mt: 1
            }}
          >
            <TextField
              label="Title"
              fullWidth
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              onKeyDown={(e) => e.key === 'Enter' && handleSave()}
              autoFocus
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Description"
              fullWidth
              multiline
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Location"
              fullWidth
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              variant="outlined"
              sx={{ 
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            />
            <TextField
              label="Category"
              select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              slotProps={{
                select: {
                  renderValue: (value: unknown) => {
                  const selected = categories.find((item) => item.name === value);
                  return (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      {selected && <Box sx={{ display: 'flex', gap: 0.25 }}>
                        <Box sx={{ width: 16, height: 16, borderRadius: 0.5, backgroundColor: selected.primaryColor }} />
                        <Box sx={{ width: 16, height: 16, borderRadius: 0.5, backgroundColor: selected.secondaryColor }} />
                      </Box>}
                      {selected?.icon} {String(value)}
                      {selected?.type && <Typography component="span" variant="caption" color="text.secondary">({selected.type})</Typography>}
                    </Box>
                  );
                  }
                }
              }}
              variant="outlined"
              sx={{ 
                width: { xs: '100%', sm: 360 },
                '& .MuiOutlinedInput-root': {
                  borderRadius: 1,
                  '& fieldset': { borderColor: theme.palette.divider },
                  '&:hover fieldset': { borderColor: theme.palette.primary.main },
                  '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                }
              }}
            >
              {!categories.some((item) => item.name === category) && (
                <MenuItem value={category}>📌 {category}</MenuItem>
              )}
              {categories.map((item) => (
                <MenuItem key={item.name} value={item.name} sx={{ borderLeft: `4px solid ${item.primaryColor}`, pl: 1.5 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%' }}>
                    <Box sx={{ display: 'flex', gap: 0.25 }}>
                      <Box sx={{ width: 18, height: 18, borderRadius: 0.5, backgroundColor: item.primaryColor }} />
                      <Box sx={{ width: 18, height: 18, borderRadius: 0.5, backgroundColor: item.secondaryColor }} />
                    </Box>
                    <span>{item.icon} {item.name}</span>
                    <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
                      {item.type}
                    </Typography>
                  </Box>
                </MenuItem>
              ))}
            </TextField>
            <FormControlLabel
              control={<Switch checked={hasTime} onChange={(event) => setHasTime(event.target.checked)} />}
              label="Set a specific time"
            />
            {hasTime ? (
              <DateTimePicker
                label="Event date and time"
                value={date}
                onChange={handleDateChange}
                ampm
                minutesStep={15}
                slotProps={{
                  textField: {
                    fullWidth: true,
                    variant: 'outlined',
                    sx: {
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 1,
                        '& fieldset': { borderColor: theme.palette.divider },
                        '&:hover fieldset': { borderColor: theme.palette.primary.main },
                        '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                      }
                    }
                  }
                }}
              />
            ) : (
              <DatePicker
                label="Event date"
                value={date}
                onChange={handleDateChange}
                slotProps={{
                  textField: {
                    fullWidth: true,
                    variant: 'outlined',
                    sx: {
                      '& .MuiOutlinedInput-root': {
                        borderRadius: 1,
                        '& fieldset': { borderColor: theme.palette.divider },
                        '&:hover fieldset': { borderColor: theme.palette.primary.main },
                        '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                      }
                    }
                  }
                }}
              />
            )}
            <FormControlLabel
              control={<Switch checked={hasEndDate} onChange={(event) => {
                setHasEndDate(event.target.checked);
                if (event.target.checked && !endDate) setEndDate(date ? new Date(date) : new Date());
                if (!event.target.checked) setHasEndTime(false);
              }} />}
              label="Set an end date"
            />
            {hasEndDate && (
              <>
                <FormControlLabel
                  control={<Switch checked={hasEndTime} onChange={(event) => setHasEndTime(event.target.checked)} />}
                  label="Set a specific end time"
                />
                {hasEndTime ? (
                  <DateTimePicker
                    label="End date and time"
                    value={endDate}
                    onChange={setEndDate}
                    ampm
                    minutesStep={15}
                    slotProps={{
                      textField: {
                        fullWidth: true,
                        variant: 'outlined',
                        sx: {
                          '& .MuiOutlinedInput-root': {
                            borderRadius: 1,
                            '& fieldset': { borderColor: theme.palette.divider },
                            '&:hover fieldset': { borderColor: theme.palette.primary.main },
                            '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                          }
                        }
                      }
                    }}
                  />
                ) : (
                  <DatePicker
                    label="End date"
                    value={endDate}
                    onChange={setEndDate}
                    slotProps={{
                      textField: {
                        fullWidth: true,
                        variant: 'outlined',
                        sx: {
                          '& .MuiOutlinedInput-root': {
                            borderRadius: 1,
                            '& fieldset': { borderColor: theme.palette.divider },
                            '&:hover fieldset': { borderColor: theme.palette.primary.main },
                            '&.Mui-focused fieldset': { borderColor: theme.palette.primary.main }
                          }
                        }
                      }
                    }}
                  />
                )}
              </>
            )}
            <FormControlLabel
              control={<Switch checked={isPublic} onChange={(event) => setIsPublic(event.target.checked)} />}
              label="Make this event public"
            />
            <Box sx={{ mt: 1, borderTop: `1px solid ${theme.palette.divider}`, pt: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
              <Box>
                <Typography variant="h6">Journal</Typography>
                <Typography variant="body2" color="text.secondary">
                  {journal.trim() ? 'Journal entry saved with this event.' : 'No journal entry yet.'}
                </Typography>
              </Box>
              <Button variant="outlined" onClick={() => setJournalOpen(true)}>Open Journal Editor</Button>
            </Box>
          </Box>
        </DialogContent>
        <DialogActions sx={{ 
          justifyContent: 'space-between',
          px: 2 
        }}>
          <Button 
            onClick={closeEventEditor}
            sx={{ 
              color: theme.palette.text.primary,
              '&:hover': { backgroundColor: theme.palette.action.hover, color: theme.palette.text.primary }
            }}
          >Cancel</Button>
          <Button onClick={handleSave} variant="contained" sx={{ backgroundColor: theme.palette.primary.main }}>Save</Button>
        </DialogActions>
      </Dialog>
      <Dialog
        open={journalOpen}
        onClose={() => setJournalOpen(false)}
        fullWidth
        maxWidth="lg"
        sx={{ zIndex: (theme) => theme.zIndex.modal + 1 }}
      >
        <DialogTitle>Journal Editor</DialogTitle>
        <DialogContent dividers>
          <Tabs value={journalTab} onChange={(_, value) => setJournalTab(value)} sx={{ mb: 2 }}>
            <Tab label="Write" />
            <Tab label="Preview" />
          </Tabs>
          {journalTab === 0 ? (
            <>
              <Stack direction="row" spacing={0.5} sx={{ mb: 1, flexWrap: 'wrap' }}>
                <Tooltip title="Heading"><Button size="small" onClick={() => insertMarkdown('# ', '')}>H1</Button></Tooltip>
                <Tooltip title="Bold"><Button size="small" onClick={() => insertMarkdown('**', '**')}><strong>B</strong></Button></Tooltip>
                <Tooltip title="Italic"><Button size="small" onClick={() => insertMarkdown('*', '*')}><em>I</em></Button></Tooltip>
                <Tooltip title="Bulleted list"><Button size="small" onClick={() => insertMarkdown('- ', '')}>List</Button></Tooltip>
                <Tooltip title="Quote"><Button size="small" onClick={() => insertMarkdown('> ', '')}>Quote</Button></Tooltip>
              </Stack>
              <TextField
                inputRef={journalInputRef}
                label="Journal entry (Markdown supported)"
                fullWidth
                multiline
                minRows={18}
                value={journal}
                onChange={(event) => setJournal(event.target.value)}
                placeholder="Write your thoughts here..."
                helperText="Use the buttons above or Markdown such as ## Heading and **bold**."
              />
            </>
          ) : (
            <Box sx={{ minHeight: 420, p: 2, border: `1px solid ${theme.palette.divider}`, borderRadius: 1, textAlign: 'left', '& h1, & h2, & h3': { color: theme.palette.text.primary }, '& p': { whiteSpace: 'pre-wrap' } }}>
              {journal.trim() ? <ReactMarkdown remarkPlugins={[remarkGfm]}>{journal}</ReactMarkdown> : <Typography color="text.secondary">No journal text yet.</Typography>}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setJournalOpen(false)}>Done</Button>
        </DialogActions>
      </Dialog>
    </LocalizationProvider>
  );
};

export default EventDialog;
